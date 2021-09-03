using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class OrderSynchronizer : IOrderSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;
        private readonly ITradingRepository _repository;

        public OrderSynchronizer(ILogger<OrderSynchronizer> logger, ISystemClock clock, ITradingService trader, ITradingRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task SynchronizeOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var watch = Stopwatch.StartNew();
            var count = 0;

            // start from the first known transient order if possible
            var orderId = await _repository
                .GetMinTransientOrderIdAsync(symbol, cancellationToken)
                .ConfigureAwait(false) - 1;

            // otherwise start from the max paged order
            if (orderId < 1)
            {
                orderId = await _repository
                    .GetLastPagedOrderIdAsync(symbol, cancellationToken)
                    .ConfigureAwait(false);
            }

            // pull all new or updated orders page by page
            while (!cancellationToken.IsCancellationRequested)
            {
                var orders = await _trader
                    .GetAllOrdersAsync(new GetAllOrders(symbol, orderId + 1, null, null, 1000, null, _clock.UtcNow), cancellationToken)
                    .ConfigureAwait(false);

                // break if we got all orders
                if (orders.Count is 0) break;

                // persist only orders that have progressed - the repository will detect which ones have updated or not
                await _repository
                    .SetOrdersAsync(orders, cancellationToken)
                    .ConfigureAwait(false);

                // keep the last order id
                orderId = orders.Max!.OrderId;

                // keep track for logging
                count += orders.Count;
            }

            if (count > 0)
            {
                // save the last paged order to continue from there next time
                await _repository
                    .SetLastPagedOrderIdAsync(symbol, orderId, cancellationToken)
                    .ConfigureAwait(false);
            }

            // log the activity only if necessary
            _logger.LogInformation(
                "{Name} {Symbol} pulled {Count} orders up to OrderId {MaxOrderId} in {ElapsedMs}ms",
                nameof(OrderSynchronizer), symbol, count, orderId, watch.ElapsedMilliseconds);
        }
    }
}