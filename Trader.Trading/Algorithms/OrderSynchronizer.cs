using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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

        public Task SynchronizeOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return SynchronizeOrdersCoreAsync(symbol, cancellationToken);
        }

        private async Task SynchronizeOrdersCoreAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var watch = Stopwatch.StartNew();
            var count = 0;

            // attempt to start from the first known transient order
            var orderId = await _orders.TryGetMinTransientOrderIdAsync(symbol, cancellationToken).ConfigureAwait(false);

            // otherwise attempt to start from the last known order
            if (!orderId.HasValue)
            {
                orderId = await _orders.TryGetMaxOrderIdAsync(symbol, cancellationToken).ConfigureAwait(false);
            }

            // otherwise start from scratch
            if (!orderId.HasValue)
            {
                orderId = 0;
            }

            // this worker will publish incoming orders in the background
            var worker = new ActionBlock<(string Symbol, IEnumerable<OrderQueryResult> Items)>(work =>
            {
                return _orders.SetOrdersAsync(work.Symbol, work.Items, cancellationToken);
            });

            // pull all new or updated orders page by page
            while (!cancellationToken.IsCancellationRequested)
            {
                var orders = await _trader
                    .GetAllOrdersAsync(symbol, orderId + 1, 1000, cancellationToken)
                    .ConfigureAwait(false);

                // break if we got all orders
                if (orders.Count is 0) break;

                // push the orders to the worker for publishing
                worker.Post((symbol, orders));

                // keep the last order id
                orderId = orders.Max(x => x.OrderId);

                // keep track for logging
                count += orders.Count;
            }

            // wait for publishing to complete
            worker.Complete();
            await worker.Completion.ConfigureAwait(false);

            // log the activity only if necessary
            _logger.LogInformation(
                "{Name} {Symbol} pulled {Count} orders up to OrderId {MaxOrderId} in {ElapsedMs}ms",
                nameof(OrderSynchronizer), symbol, count, orderId, watch.ElapsedMilliseconds);
        }
    }
}