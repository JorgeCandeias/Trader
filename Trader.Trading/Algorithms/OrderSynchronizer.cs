using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class OrderSynchronizer : IOrderSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly IOrderProvider _orders;

        public OrderSynchronizer(ILogger<OrderSynchronizer> logger, ITradingService trader, IOrderProvider orders)
        {
            _logger = logger;
            _trader = trader;
            _orders = orders;
        }

        public async Task SynchronizeOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var watch = Stopwatch.StartNew();
            var count = 0;

            // start from the first known transient order if possible
            var orderId = await _orders
                .GetMinTransientOrderIdAsync(symbol, cancellationToken)
                .ConfigureAwait(false) - 1;

            // otherwise start from the max paged order
            if (orderId < 1)
            {
                orderId = await _orders
                    .GetMaxOrderIdAsync(symbol, cancellationToken)
                    .ConfigureAwait(false);
            }

            // pull all new or updated orders page by page
            while (!cancellationToken.IsCancellationRequested)
            {
                var orders = await _trader
                    .GetAllOrdersAsync(symbol, orderId + 1, 1000, cancellationToken)
                    .ConfigureAwait(false);

                // break if we got all orders
                if (orders.Count is 0) break;

                // persist only orders that have progressed - the repository will detect which ones have updated or not
                await _orders
                    .SetOrdersAsync(symbol, orders, cancellationToken)
                    .ConfigureAwait(false);

                // keep the last order id
                orderId = orders.Max(x => x.OrderId);

                // keep track for logging
                count += orders.Count;
            }

            // log the activity only if necessary
            _logger.LogInformation(
                "{Name} {Symbol} pulled {Count} orders up to OrderId {MaxOrderId} in {ElapsedMs}ms",
                nameof(OrderSynchronizer), symbol, count, orderId, watch.ElapsedMilliseconds);
        }
    }
}