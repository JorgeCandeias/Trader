using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;
using Trader.Trading.Algorithms;
using Trader.Trading.Binance;
using static System.String;

namespace Trader.Trading
{
    internal sealed class UserDataStreamHost : IHostedService, IDisposable
    {
        private readonly UserDataStreamHostOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IUserDataStreamClientFactory _streams;
        private readonly IOrderSynchronizer _orders;
        private readonly ITradeSynchronizer _trades;
        private readonly ITraderRepository _repository;
        private readonly ISystemClock _clock;

        public UserDataStreamHost(IOptions<UserDataStreamHostOptions> options, ILogger<UserDataStreamHost> logger, ITradingService trader, IUserDataStreamClientFactory streams, IOrderSynchronizer orders, ITradeSynchronizer trades, ITraderRepository repository, ISystemClock clock)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _streams = streams ?? throw new ArgumentNullException(nameof(streams));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _trades = trades ?? throw new ArgumentNullException(nameof(trades));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Name => nameof(UserDataStreamHost);

        private readonly CancellationTokenSource _cancellation = new();
        private CancellationTokenSource? _linkedStartupCancellation;
        private readonly TaskCompletionSource _ready = new();
        private string? _listenKey;
        private Task? _workerTask;
        private DateTime _nextPingTime;

        private void BumpPingTime()
        {
            _nextPingTime = _clock.UtcNow.Add(_options.PingPeriod);
        }

        private async Task TickWorkerAsync(CancellationToken cancellationToken)
        {
            // if startup is cancelled then cancel the ready flag as well so the service fails
            using var registration = cancellationToken.Register(() =>
            {
                _ready.TrySetCanceled(cancellationToken);
            });

            _listenKey = await _trader
                .CreateUserDataStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("{Name} created user stream with key {ListenKey}", Name, _listenKey);

            using var client = _streams.Create(_listenKey);

            await client
                .ConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            BumpPingTime();

            _logger.LogInformation("{Name} connected user stream with key {ListenKey}", Name, _listenKey);

            // start streaming in the background while we sync from the api
            var streamTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_clock.UtcNow >= _nextPingTime)
                    {
                        await _trader
                            .PingUserDataStreamAsync(_listenKey, cancellationToken)
                            .ConfigureAwait(false);

                        BumpPingTime();
                    }

                    var message = await client
                        .ReceiveAsync(cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogInformation("{Name} received message {Message}", Name, message);

                    switch (message)
                    {
                        case OutboundAccountPositionUserDataStreamMessage balance:

                            var balances = balance.Balances
                                .Select(x => new Balance(x.Asset, x.Free, x.Locked, balance.LastAccountUpdateTime));

                            await _repository
                                .SetBalancesAsync(balances, cancellationToken)
                                .ConfigureAwait(false);

                            break;

                        case BalanceUpdateUserDataStreamMessage update:

                            // noop

                            break;

                        case ExecutionReportUserDataStreamMessage report:

                            if (!_options.Symbols.Contains(report.Symbol))
                            {
                                _logger.LogWarning(
                                    "{Name} ignoring {MessageType} for unknown symbol {Symbol}",
                                    Name, nameof(ExecutionReportUserDataStreamMessage), report.Symbol);

                                break;
                            }

                            // first extract the trade from this report if any
                            // this must be persisted before the order so concurrent algos can pick up consistent data based on the order
                            // todo: push this to the repository as a single transacted operation to improve consistency
                            // todo: move this to automapper
                            if (report.ExecutionType == ExecutionType.Trade)
                            {
                                var trade = new AccountTrade(
                                    report.Symbol,
                                    report.TradeId,
                                    report.OrderId,
                                    report.OrderListId,
                                    report.LastExecutedPrice,
                                    report.LastExecutedQuantity,
                                    report.LastQuoteAssetTransactedQuantity,
                                    report.CommissionAmount,
                                    report.CommissionAsset,
                                    report.TransactionTime,
                                    report.OrderSide == OrderSide.Buy,
                                    report.IsMakerOrder,
                                    true);

                                await _repository
                                    .SetTradeAsync(trade)
                                    .ConfigureAwait(false);
                            }

                            // now extract the order from this report
                            // todo: move this to automapper
                            var order = new OrderQueryResult(
                                report.Symbol,
                                report.OrderId,
                                report.OrderListId,
                                IsNullOrWhiteSpace(report.OriginalClientOrderId) ? report.ClientOrderId : report.OriginalClientOrderId,
                                report.OrderPrice,
                                report.OrderQuantity,
                                report.CummulativeFilledQuantity,
                                report.CummulativeQuoteAssetTransactedQuantity,
                                report.OrderStatus,
                                report.TimeInForce,
                                report.OrderType,
                                report.OrderSide,
                                report.StopPrice,
                                report.IcebergQuantity,
                                report.OrderCreatedTime,
                                report.TransactionTime,
                                true,
                                report.QuoteOrderQuantity);

                            await _repository
                                .SetOrderAsync(order, cancellationToken)
                                .ConfigureAwait(false);

                            break;

                        case ListStatusUserDataStreamMessage list:
                            break;

                        default:
                            _logger.LogWarning("{Name} received unknown message {Message}", Name, message);
                            break;
                    }
                }
            }, cancellationToken);

            // wait for a few seconds for the stream to stabilize so we don't miss any incoming data from binance
            _logger.LogInformation("{Name} waiting {Period} for stream to stabilize...", Name, _options.StabilizationPeriod);
            await Task.Delay(_options.StabilizationPeriod, cancellationToken).ConfigureAwait(false);

            // sync asset balances
            var accountInfo = await _trader
                .GetAccountInfoAsync(new GetAccountInfo(null, _clock.UtcNow), cancellationToken)
                .ConfigureAwait(false);

            await _repository
                .SetBalancesAsync(accountInfo, cancellationToken)
                .ConfigureAwait(false);

            // sync orders for all symbols
            foreach (var symbol in _options.Symbols)
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
                    .ExecuteAsync(ct => _orders.SynchronizeOrdersAsync(symbol, ct), cancellationToken, false)
                    .ConfigureAwait(false);
            }

            // sync trades for all symbols
            foreach (var symbol in _options.Symbols)
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
                    .ExecuteAsync(ct => _trades.SynchronizeTradesAsync(symbol, cancellationToken), cancellationToken, false)
                    .ConfigureAwait(false);
            }

            // signal the start method that everything is ready
            _ready.TrySetResult();

            // keep streaming now
            await streamTask.ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} starting...", Name);

            // combine the startup cancellation so we can handle early process start cancellations properly
            _linkedStartupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellation.Token);

            // now start the worker policy
            _workerTask = Policy
                .Handle<Exception>(ex =>
                {
                    _logger.LogError(ex, "{Name} handled work exception {Message}", Name, ex.Message);
                    return true;
                })
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(10))
                .ExecuteAsync(TickWorkerAsync, _linkedStartupCancellation.Token, false);

            // wait for everything to sync before letting other services start
            await _ready.Task.ConfigureAwait(false);

            _logger.LogInformation("{Name} started", Name);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} stopping...", Name);

            // cancel any background work
            _cancellation.Cancel();

            // await for the worker task to complete
            try
            {
                if (_workerTask is not null)
                {
                    await _workerTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // noop
            }

            // gracefully unregister the user stream
            if (_listenKey is not null)
            {
                await _trader
                    .CloseUserDataStreamAsync(_listenKey, cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation("{Name} stopped", Name);
        }

        #region Disposable

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _cancellation.Dispose();
                _linkedStartupCancellation?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UserDataStreamHost()
        {
            Dispose(false);
        }

        #endregion Disposable
    }
}