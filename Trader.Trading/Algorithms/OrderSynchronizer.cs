using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    internal class OrderSynchronizer : IOrderSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;
        private readonly ITraderRepository _repository;

        public OrderSynchronizer(ILogger<OrderSynchronizer> logger, ISystemClock clock, ITradingService trader, ITraderRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task SynchronizeOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var watch = Stopwatch.StartNew();

            // start with the minimum transient order if there is any
            var orderId = await _repository.GetMinTransientOrderIdAsync(symbol, cancellationToken);

            // otherwise start after the last order
            if (orderId == 0)
            {
                orderId = await _repository.GetMaxOrderIdAsync(symbol, cancellationToken) + 1;
            }

            // pull all new or updated orders page by page
            var count = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var orders = await _trader.GetAllOrdersAsync(new GetAllOrders(symbol, orderId, null, null, 1000, null, _clock.UtcNow), cancellationToken);

                // stop if we got all orders
                if (orders.Count is 0) break;

                // persist all new and updated orders
                await _repository.SetOrdersAsync(orders, cancellationToken);

                // set the start of the next page
                orderId = orders.Max!.OrderId + 1;

                // keep track for logging
                count += orders.Count;
            }

            // log the activity only if necessary
            if (count > 0)
            {
                _logger.LogInformation(
                    "{Name} {Symbol} pulled {Count} orders up to OrderId {MaxOrderId} in {ElapsedMs}ms",
                    nameof(OrderSynchronizer), symbol, count, orderId, watch.ElapsedMilliseconds);
            }
        }
    }
}