using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using Polly;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IOrderProvider _orderProvider;
        private readonly IBalanceProvider _balances;

        public BinanceUserDataGrain(IOptions<BinanceOptions> options, ILogger<BinanceUserDataGrain> logger, ITradingService trader, IUserDataStreamClientFactory streams, IOrderSynchronizer orders, ITradeSynchronizer trades, ITradingRepository repository, ISystemClock clock, IMapper mapper, IHostApplicationLifetime lifetime, IOrderProvider orderProvider, IBalanceProvider balances)
        {
            _options = options.Value;
            _logger = logger;
            _trader = trader;
            _streams = streams;
            _orders = orders;
            _trades = trades;
            _repository = repository;
            _clock = clock;
            _mapper = mapper;
            _lifetime = lifetime;
            _orderProvider = orderProvider;
            _balances = balances;
        }

        private static string Name => nameof(BinanceUserDataGrain);

        private string? _listenKey;

        private DateTime _nextPingTime;

        private bool _ready;

        /// <summary>
        /// Holds the background streaming and syncing work.
        /// </summary>
        private Task? _work;

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => TickEnsureWorkAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.LogInformation("{Name} started", Name);

            return base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
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
            if (!_options.UserDataStreamSymbols.Contains(message.Symbol))
            {
                _logger.LogWarning(
                    "{Name} ignoring {MessageType} for unknown symbol {Symbol}",
                    Name, nameof(ExecutionReportUserDataStreamMessage), message.Symbol);

                return;
            }

            // first extract the trade from this report if any
            // this must be persisted before the order so concurrent algos can pick up consistent data based on the order
            if (message.ExecutionType == ExecutionType.Trade)
            {
                var trade = _mapper.Map<AccountTrade>(message);

                await _repository.SetTradeAsync(trade, _lifetime.ApplicationStopping);

                _logger.LogInformation(
                    "{Name} saved {Symbol} {Side} trade {TradeId}",
                    Name, message.Symbol, message.OrderSide, message.TradeId);
            }

            // now extract the order from this report
            var order = _mapper.Map<OrderQueryResult>(message);

            await _orderProvider.SetOrderAsync(order);

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
            try
            {
                _logger.LogInformation("{Name} creating user stream key...", Name);

                _listenKey = await _trader.CreateUserDataStreamAsync(_lifetime.ApplicationStopping);

                _logger.LogInformation("{Name} created user stream with key {ListenKey}", Name, _listenKey);

                _logger.LogInformation("{Name} connecting user stream with key {ListenKey}...", Name, _listenKey);

                using var client = _streams.Create(_listenKey);

                await client.ConnectAsync(_lifetime.ApplicationStopping);

                BumpPingTime();

                _logger.LogInformation("{Name} connected user stream with key {ListenKey}", Name, _listenKey);

                // this worker will process and publish messages in the background so we dont hold up the binance stream
                var worker = new ActionBlock<UserDataStreamMessage>(HandleMessageAsync, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

                // start streaming in the background while we sync from the api
                var streamTask = Task.Run(async () =>
                {
                    while (!_lifetime.ApplicationStopping.IsCancellationRequested)
                    {
                        if (_clock.UtcNow >= _nextPingTime)
                        {
                            await _trader.PingUserDataStreamAsync(_listenKey, _lifetime.ApplicationStopping);

                            BumpPingTime();
                        }

                        var message = await client.ReceiveAsync(_lifetime.ApplicationStopping);

                        worker.Post(message);
                    }
                }, _lifetime.ApplicationStopping);

                // wait for a few seconds for the stream to stabilize so we don't miss any incoming data from binance
                _logger.LogInformation("{Name} waiting {Period} for stream to stabilize...", Name, _options.UserDataStreamStabilizationPeriod);
                await Task.Delay(_options.UserDataStreamStabilizationPeriod, _lifetime.ApplicationStopping);

                // sync asset balances
                var accountInfo = await _trader.GetAccountInfoAsync(_lifetime.ApplicationStopping);

                await _balances.SetBalancesAsync(accountInfo, _lifetime.ApplicationStopping);

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
                        .ExecuteAsync(ct => _orders.SynchronizeOrdersAsync(symbol, ct), _lifetime.ApplicationStopping, true);
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
                        .ExecuteAsync(ct => _trades.SynchronizeTradesAsync(symbol, ct), _lifetime.ApplicationStopping, true);
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
    }
}