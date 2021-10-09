using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Polly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData
{
    internal class BinanceUserDataGrain : Grain, IBinanceUserDataGrain
    {
        private readonly BinanceOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IUserDataStreamClientFactory _streams;
        private readonly IOrderSynchronizer _orders;
        private readonly ITradeSynchronizer _trades;
        private readonly ITradingRepository _repository;
        private readonly ISystemClock _clock;
        private readonly IMapper _mapper;

        public BinanceUserDataGrain(IOptions<BinanceOptions> options, ILogger<BinanceUserDataGrain> logger, ITradingService trader, IUserDataStreamClientFactory streams, IOrderSynchronizer orders, ITradeSynchronizer trades, ITradingRepository repository, ISystemClock clock, IMapper mapper)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _streams = streams ?? throw new ArgumentNullException(nameof(streams));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _trades = trades ?? throw new ArgumentNullException(nameof(trades));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        private static string Name => nameof(BinanceUserDataGrain);

        private readonly CancellationTokenSource _cancellation = new();
        private string? _listenKey;
        private DateTime _nextPingTime;

        private readonly Channel<OutboundAccountPositionUserDataStreamMessage> _balancesChannel = Channel.CreateUnbounded<OutboundAccountPositionUserDataStreamMessage>();
        private readonly Channel<ExecutionReportUserDataStreamMessage> _executionChannel = Channel.CreateUnbounded<ExecutionReportUserDataStreamMessage>();

        private bool _ready;

        /// <summary>
        /// Holds the background streaming and syncing work.
        /// </summary>
        private Task? _work;

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => TickEnsureWorkAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            RegisterTimer(_ => TickSaveBalancesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            RegisterTimer(_ => TickSaveExecutionsAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInformation("{Name} started", Name);

            return base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            _cancellation.Dispose();

            // gracefully unregister the user stream
            if (_listenKey is not null)
            {
                await _trader.CloseUserDataStreamAsync(_listenKey, _cancellation.Token);
            }

            await base.OnDeactivateAsync();
        }

        /// <summary>
        /// Monitors the background streaming work task and ensures it remains active upon faulting.
        /// </summary>
        private async Task TickEnsureWorkAsync()
        {
            // avoid starting streaming work upon shutdown
            if (_cancellation.IsCancellationRequested)
            {
                return;
            }

            // schedule streaming work if nothing is running
            if (_work is null)
            {
                _work = Task.Run(() => ExecuteLongAsync(), _cancellation.Token);
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

        public Task<bool> IsReadyAsync() => Task.FromResult(_ready);

        private void BumpPingTime()
        {
            _nextPingTime = _clock.UtcNow.Add(_options.UserDataStreamPingPeriod);
        }

        private async Task ExecuteLongAsync()
        {
            try
            {
                _logger.LogInformation("{Name} creating user stream key...", Name);

                _listenKey = await _trader.CreateUserDataStreamAsync(_cancellation.Token);

                _logger.LogInformation("{Name} created user stream with key {ListenKey}", Name, _listenKey);

                _logger.LogInformation("{Name} connecting user stream with key {ListenKey}...", Name, _listenKey);

                using var client = _streams.Create(_listenKey);

                await client.ConnectAsync(_cancellation.Token);

                BumpPingTime();

                _logger.LogInformation("{Name} connected user stream with key {ListenKey}", Name, _listenKey);

                // start streaming in the background while we sync from the api
                var streamTask = Task.Run(async () =>
                {
                    while (!_cancellation.Token.IsCancellationRequested)
                    {
                        if (_clock.UtcNow >= _nextPingTime)
                        {
                            await _trader.PingUserDataStreamAsync(_listenKey, _cancellation.Token);

                            BumpPingTime();
                        }

                        var message = await client.ReceiveAsync(_cancellation.Token);

                        switch (message)
                        {
                            case OutboundAccountPositionUserDataStreamMessage balance:

                                await _balancesChannel.Writer.WriteAsync(balance, _cancellation.Token);

                                break;

                            case BalanceUpdateUserDataStreamMessage update:

                                // noop

                                break;

                            case ExecutionReportUserDataStreamMessage report:

                                await _executionChannel.Writer.WriteAsync(report, _cancellation.Token);

                                break;

                            case ListStatusUserDataStreamMessage list:
                                break;

                            default:
                                _logger.LogWarning("{Name} received unknown message {Message}", Name, message);
                                break;
                        }
                    }
                }, _cancellation.Token);

                // wait for a few seconds for the stream to stabilize so we don't miss any incoming data from binance
                _logger.LogInformation("{Name} waiting {Period} for stream to stabilize...", Name, _options.UserDataStreamStabilizationPeriod);
                await Task.Delay(_options.UserDataStreamStabilizationPeriod, _cancellation.Token);

                // sync asset balances
                var accountInfo = await _trader.GetAccountInfoAsync(new GetAccountInfo(null, _clock.UtcNow), _cancellation.Token);

                await _repository.SetBalancesAsync(accountInfo, _cancellation.Token);

                // sync orders for all symbols
                foreach (var symbol in _options.UserDataStreamSymbols)
                {
                    await Policy
                        .Handle<BinanceTooManyRequestsException>()
                        .WaitAndRetryForeverAsync(
                            (n, ex, ctx) => ((BinanceTooManyRequestsException)ex).RetryAfter.Add(TimeSpan.FromSeconds(1)),
                            (ex, ts, ctx) =>
                            {
                                _logger.LogWarning(ex,
                                    "{Name} backing off for {TimeSpan}...",
                                    Name, ts.Add(TimeSpan.FromSeconds(1)));

                                return Task.CompletedTask;
                            })
                        .ExecuteAsync(ct => _orders.SynchronizeOrdersAsync(symbol, ct), _cancellation.Token, true);
                }

                // sync trades for all symbols
                foreach (var symbol in _options.UserDataStreamSymbols)
                {
                    await Policy
                        .Handle<BinanceTooManyRequestsException>()
                        .WaitAndRetryForeverAsync(
                            (n, ex, ctx) => ((BinanceTooManyRequestsException)ex).RetryAfter,
                            (ex, ts, ctx) =>
                            {
                                _logger.LogWarning(ex,
                                    "{Name} backing off for {TimeSpan}...",
                                    Name, ts);

                                return Task.CompletedTask;
                            })
                        .ExecuteAsync(ct => _trades.SynchronizeTradesAsync(symbol, _cancellation.Token), _cancellation.Token, true);
                }

                // signal that everything is ready
                _ready = true;

                // keep streaming now
                await streamTask;
            }
            finally
            {
                // if the stream collapses at any point then update the readyness state
                _ready = false;
            }
        }

        private async Task TickSaveBalancesAsync()
        {
            while (!_cancellation.Token.IsCancellationRequested)
            {
                if (!await _balancesChannel.Reader.WaitToReadAsync(_cancellation.Token))
                {
                    return;
                }

                while (_balancesChannel.Reader.TryRead(out var message))
                {
                    var balances = message.Balances.Select(x => new Balance(x.Asset, x.Free, x.Locked, message.LastAccountUpdateTime));

                    await _repository.SetBalancesAsync(balances, _cancellation.Token);

                    _logger.LogInformation("{Name} saved balances for {Assets}", Name, message.Balances.Select(x => x.Asset));
                }
            }
        }

        private async Task TickSaveExecutionsAsync()
        {
            while (!_cancellation.Token.IsCancellationRequested)
            {
                if (!await _executionChannel.Reader.WaitToReadAsync(_cancellation.Token))
                {
                    return;
                }

                while (_executionChannel.Reader.TryRead(out var report))
                {
                    if (!_options.UserDataStreamSymbols.Contains(report.Symbol))
                    {
                        _logger.LogWarning(
                            "{Name} ignoring {MessageType} for unknown symbol {Symbol}",
                            Name, nameof(ExecutionReportUserDataStreamMessage), report.Symbol);

                        break;
                    }

                    // first extract the trade from this report if any
                    // this must be persisted before the order so concurrent algos can pick up consistent data based on the order
                    if (report.ExecutionType == ExecutionType.Trade)
                    {
                        var trade = _mapper.Map<AccountTrade>(report);

                        await _repository.SetTradeAsync(trade, _cancellation.Token);

                        _logger.LogInformation(
                            "{Name} saved {Symbol} {Side} trade {TradeId}",
                            Name, report.Symbol, report.OrderSide, report.TradeId);
                    }

                    // now extract the order from this report
                    var order = _mapper.Map<OrderQueryResult>(report);

                    await _repository.SetOrderAsync(order, _cancellation.Token);

                    _logger.LogInformation(
                        "{Name} saved {Symbol} {Type} {Side} order {OrderId}",
                        Name, report.Symbol, report.OrderType, report.OrderSide, report.OrderId);
                }
            }
        }
    }
}