using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;
using Trader.Data;
using Trader.Models;
using Trader.Trading.Algorithms;
using static System.String;

namespace Trader.Trading
{
    internal sealed class UserDataStreamHost : IHostedService, IDisposable
    {
        private readonly UserDataStreamHostOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IUserDataStreamClient _client;
        private readonly ISafeTimerFactory _timers;
        private readonly IOrderSynchronizer _orders;
        private readonly ITradeSynchronizer _trades;
        private readonly ITraderRepository _repository;
        private readonly ISystemClock _clock;

        public UserDataStreamHost(IOptions<UserDataStreamHostOptions> options, ILogger<UserDataStreamHost> logger, ITradingService trader, IUserDataStreamClient client, ISafeTimerFactory timers, IOrderSynchronizer orders, ITradeSynchronizer trades, ITraderRepository repository, ISystemClock clock)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _timers = timers ?? throw new ArgumentNullException(nameof(timers));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _trades = trades ?? throw new ArgumentNullException(nameof(trades));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Name => nameof(UserDataStreamHost);
        private readonly TaskCompletionSource _ready = new TaskCompletionSource();
        private string _listenKey = Empty;
        private ISafeTimer? _workerTimer;

        private async Task TickWorkerAsync(CancellationToken cancellationToken)
        {
            _listenKey = await _trader
                .CreateUserDataStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("{Name} created user stream with key {ListenKey}", Name, _listenKey);

            await _client
                .ConnectAsync(_listenKey, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("{Name} connected user stream with key {ListenKey}", Name, _listenKey);

            // start streaming in the background while we sync from the api
            var streamTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // todo: ping the stream every ten minutes


                    var message = await _client
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
                            // todo: this needs the repository to support balances
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
                                report.OriginalClientOrderId ?? report.ClientOrderId,
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
                await _orders
                    .SynchronizeOrdersAsync(symbol, cancellationToken)
                    .ConfigureAwait(false);
            }

            // sync trades for all symbols
            foreach (var symbol in _options.Symbols)
            {
                await _trades
                    .SynchronizeTradesAsync(symbol, cancellationToken)
                    .ConfigureAwait(false);
            }

            // signal the start method that everything is ready
            _ready.SetResult();

            // keep streaming now
            await streamTask.ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} starting...", Name);

            // spin up the worker timer
            _workerTimer = _timers.Create(TickWorkerAsync, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);

            // wait for everything to sync
            await _ready.Task.ConfigureAwait(false);

            _logger.LogInformation("{Name} started", Name);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} stopping...", Name);

            // stop recovery from ticking again
            _workerTimer?.Dispose();

            // gracefully close the web socket
            await _client
                .CloseAsync(cancellationToken)
                .ConfigureAwait(false);

            // gracefully unregister the user stream
            await _trader
                .CloseUserDataStreamAsync(_listenKey, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("{Name} stopped", Name);
        }

        #region Disposable

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _workerTimer?.Dispose();
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