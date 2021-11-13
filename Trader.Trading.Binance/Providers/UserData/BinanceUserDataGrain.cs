﻿using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData
{
    internal class BinanceUserDataGrain : Grain, IBinanceUserDataGrain
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

        private static string Name => nameof(BinanceUserDataGrain);

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

            _logger.LogInformation("{Name} started", Name);

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

            _logger.LogInformation("{Name} saved balances for {Assets}", Name, message.Balances.Select(x => x.Asset));
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

                _logger.LogInformation(
                    "{Name} saved {Symbol} {Side} trade {TradeId}",
                    Name, message.Symbol, message.OrderSide, message.TradeId);
            }

            // now extract the order from this report
            var order = _mapper.Map<OrderQueryResult>(message);

            await _orders.SetOrderAsync(order);

            _logger.LogInformation(
                "{Name} saved {Symbol} {Type} {Side} order {OrderId}",
                Name, message.Symbol, message.OrderType, message.OrderSide, message.OrderId);
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
                        _logger.LogWarning("{Name} received unknown message {Message}", Name, message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Name} failed to publish {Message}", Name, message);
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
            _logger.LogInformation("{Name} creating user stream key...", Name);

            _listenKey = await _trader.CreateUserDataStreamAsync(cancellationToken);

            _logger.LogInformation("{Name} created user stream with key {ListenKey}", Name, _listenKey);

            _logger.LogInformation("{Name} connecting user stream with key {ListenKey}...", Name, _listenKey);

            using var client = _streams.Create(_listenKey);

            await client.ConnectAsync(cancellationToken);

            BumpPingTime();

            _logger.LogInformation("{Name} connected user stream with key {ListenKey}", Name, _listenKey);

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
    }
}