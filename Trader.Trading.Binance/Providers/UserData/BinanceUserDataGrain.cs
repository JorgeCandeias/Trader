using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData;

internal partial class BinanceUserDataGrain : Grain, IBinanceUserDataGrain
{
    private readonly BinanceOptions _options;
    private readonly IAlgoDependencyResolver _dependencies;
    private readonly ILogger _logger;
    private readonly ITradingService _trader;
    private readonly IUserDataStreamClientFactory _streams;
    private readonly IOrderSynchronizer _orderSynchronizer;
    private readonly ITradeSynchronizer _tradeSynchronizer;
    private readonly ISystemClock _clock;
    private readonly IMapper _mapper;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IOrderProvider _orders;
    private readonly IBalanceProvider _balances;
    private readonly ITradeProvider _trades;
    private readonly ITimerRegistry _timers;

    public BinanceUserDataGrain(IOptions<BinanceOptions> options, IAlgoDependencyResolver dependencies, ILogger<BinanceUserDataGrain> logger, ITradingService trader, IUserDataStreamClientFactory streams, IOrderSynchronizer orders, ITradeSynchronizer trades, ISystemClock clock, IMapper mapper, IHostApplicationLifetime lifetime, IOrderProvider orderProvider, IBalanceProvider balances, ITradeProvider tradeProvider, ITimerRegistry timers)
    {
        _options = options.Value;
        _dependencies = dependencies;
        _logger = logger;
        _trader = trader;
        _streams = streams;
        _orderSynchronizer = orders;
        _tradeSynchronizer = trades;
        _clock = clock;
        _mapper = mapper;
        _lifetime = lifetime;
        _orders = orderProvider;
        _balances = balances;
        _trades = tradeProvider;
        _timers = timers;

        _pusher = new ActionBlock<UserDataStreamMessage>(HandleMessageAsync, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _dependencies.AllSymbols.Count * 2 + 1 });
    }

    private const string TypeName = nameof(BinanceUserDataGrain);

    private string? _listenKey;

    private DateTime _nextPingTime;

    private bool _ready;

    /// <summary>
    /// Holds the background streaming and syncing work.
    /// </summary>
    private Task? _work;

    // this worker will process and publish messages in the background so we dont hold up the binance stream
    private readonly ActionBlock<UserDataStreamMessage> _pusher;

    private IDisposable? _timer;

    public override Task OnActivateAsync()
    {
        _timer = _timers.RegisterTimer(this, _ => TickEnsureWorkAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        LogStarted(TypeName);

        return base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        _timer?.Dispose();

        // gracefully unregister the user stream
        if (_listenKey is not null)
        {
            await _trader.CloseUserDataStreamAsync(_listenKey, _lifetime.ApplicationStopping);
        }

        await base.OnDeactivateAsync();
    }

    /// <summary>
    /// Monitors the background streaming work task and ensures it remains active upon faulting.
    /// </summary>
    private async Task TickEnsureWorkAsync()
    {
        // avoid starting streaming work upon shutdown
        if (_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            return;
        }

        // schedule streaming work if nothing is running
        if (_work is null)
        {
            _work = Task.Run(() => ExecuteLongAsync(), _lifetime.ApplicationStopping);
            return;
        }

        // propagate any exceptions from completed streaming work and release the task
        if (_work.IsCompleted)
        {
            try
            {
                await _work;
            }
            finally
            {
                _work = null;
            }
        }
    }

    public ValueTask<bool> IsReadyAsync() => new(_ready);

    private void BumpPingTime()
    {
        _nextPingTime = _clock.UtcNow.Add(_options.UserDataStreamPingPeriod);
    }

    private async Task HandleBalanceMessageAsync(OutboundAccountPositionUserDataStreamMessage message)
    {
        var balances = message.Balances.Select(x => new Balance(x.Asset, x.Free, x.Locked, message.LastAccountUpdateTime));

        await _balances.SetBalancesAsync(balances, _lifetime.ApplicationStopping);

        LogSavedBalancesForAssets(TypeName, message.Balances.Select(x => x.Asset));
    }

    private async Task HandleReportMessageAsync(ExecutionReportUserDataStreamMessage message)
    {
        // first extract the trade from this report if any
        // this must be persisted before the order so concurrent algos can pick up consistent data based on the order
        if (message.ExecutionType == ExecutionType.Trade)
        {
            var trade = _mapper.Map<AccountTrade>(message);

            await _trades
                .SetTradeAsync(trade, _lifetime.ApplicationStopping)
                .ConfigureAwait(false);

            LogSavedTrade(TypeName, message.Symbol, message.OrderSide, message.TradeId);
        }

        // now extract the order from this report
        var order = _mapper.Map<OrderQueryResult>(message);

        await _orders.SetOrderAsync(order);

        LogSavedOrder(TypeName, message.Symbol, message.OrderType, message.OrderSide, message.OrderId);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Worker")]
    private Task HandleMessageAsync(UserDataStreamMessage message)
    {
        try
        {
            switch (message)
            {
                case OutboundAccountPositionUserDataStreamMessage balanceMessage:
                    return HandleBalanceMessageAsync(balanceMessage);

                case BalanceUpdateUserDataStreamMessage updateMessage:
                    break;

                case ExecutionReportUserDataStreamMessage reportMessage:
                    return HandleReportMessageAsync(reportMessage);

                case ListStatusUserDataStreamMessage listMessage:
                    break;

                default:
                    LogReceivedUnknownMessage(TypeName, message);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogFailedToPublishMessage(ex, TypeName, message);
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteLongAsync()
    {
        // this token will reset the stream on a schedule to compensate for binance misbehaving
        using var reset = new CancellationTokenSource(_options.UserDataStreamResetPeriod);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(reset.Token, _lifetime.ApplicationStopping);

        // start streaming in the background while we sync from the api
        var streamTask = Task.Run(() => StreamAsync(linked.Token), linked.Token);

        // sync asset balances
        var accountInfo = await _trader.GetAccountInfoAsync(linked.Token);
        await _balances.SetBalancesAsync(accountInfo, linked.Token);

        // sync orders for all symbols
        foreach (var symbol in _dependencies.AllSymbols)
        {
            await _orderSynchronizer.SynchronizeOrdersAsync(symbol, linked.Token);
        }

        // sync trades for all symbols
        foreach (var symbol in _dependencies.AllSymbols)
        {
            await _tradeSynchronizer.SynchronizeTradesAsync(symbol, linked.Token);
        }

        // signal that everything is ready
        _ready = true;

        // keep streaming now
        await streamTask;
    }

    private async Task StreamAsync(CancellationToken cancellationToken)
    {
        LogCreatingUserStreamKey(TypeName);

        _listenKey = await _trader.CreateUserDataStreamAsync(cancellationToken);

        LogCreatedUserStreamKey(TypeName, _listenKey);

        LogConnectingToUserStreamWithKey(TypeName, _listenKey);

        using var client = _streams.Create(_listenKey);

        await client.ConnectAsync(cancellationToken);

        BumpPingTime();

        LogConnectedToUserStreamWithKey(TypeName, _listenKey);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_clock.UtcNow >= _nextPingTime)
            {
                await _trader.PingUserDataStreamAsync(_listenKey, cancellationToken);

                BumpPingTime();
            }

            var message = await client.ReceiveAsync(cancellationToken);

            _pusher.Post(message);
        }
    }

    public Task PingAsync() => Task.CompletedTask;

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} started")]
    private partial void LogStarted(string type);

    [LoggerMessage(1, LogLevel.Information, "{Type} saved balances for {Assets}")]
    private partial void LogSavedBalancesForAssets(string type, IEnumerable<string> assets);

    [LoggerMessage(2, LogLevel.Information, "{Type} saved {Symbol} {OrderSide} trade {TradeId}")]
    private partial void LogSavedTrade(string type, string symbol, OrderSide orderSide, long tradeId);

    [LoggerMessage(3, LogLevel.Information, "{Type} saved {Symbol} {OrderType} {OrderSide} order {OrderId}")]
    private partial void LogSavedOrder(string type, string symbol, OrderType orderType, OrderSide orderSide, long orderId);

    [LoggerMessage(4, LogLevel.Warning, "{Type} received unknown message {Message}")]
    private partial void LogReceivedUnknownMessage(string type, UserDataStreamMessage message);

    [LoggerMessage(5, LogLevel.Error, "{Type} failed to publish {Message}")]
    private partial void LogFailedToPublishMessage(Exception ex, string type, UserDataStreamMessage message);

    [LoggerMessage(6, LogLevel.Information, "{Type} creating user stream key...")]
    private partial void LogCreatingUserStreamKey(string type);

    [LoggerMessage(7, LogLevel.Information, "{Type} created user stream with key {ListenKey}")]
    private partial void LogCreatedUserStreamKey(string type, string listenKey);

    [LoggerMessage(8, LogLevel.Information, "{Type} connecting to user stream with key {ListenKey}...")]
    private partial void LogConnectingToUserStreamWithKey(string type, string listenKey);

    [LoggerMessage(9, LogLevel.Information, "{Type} connected to user stream with key {ListenKey}")]
    private partial void LogConnectedToUserStreamWithKey(string type, string listenKey);

    #endregion Logging
}