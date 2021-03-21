using Microsoft.Extensions.Logging;
using System;
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
            // start with the minimum transient order if there is any
            var orderId = await _repository.GetMinTransientOrderIdAsync(symbol, cancellationToken);

            // otherwise start after the last order
            if (orderId == 0)
            {
                orderId = await _repository.GetMaxOrderIdAsync(symbol, cancellationToken) + 1;
            }

            // pull all new or updated orders page by page
            var count = 0;
            SortedOrderSet orders;
            do
            {
                // todo: refactor this to return a SortedOrderSet
                orders = await _trader.GetAllOrdersAsync(new GetAllOrders(symbol, orderId, null, null, 1000, null, _clock.UtcNow), cancellationToken);

                if (orders.Count > 0)
                {
                    // persist all new and updated orders
                    await _repository.SetOrdersAsync(orders, cancellationToken);

                    // set the start of the next page
                    orderId = orders.Max!.OrderId + 1;

                    // keep track for logging
                    count += orders.Count;
                }
            } while (orders.Count >= 1000);

            // log the activity only if necessary
            if (count > 0)
            {
                _logger.LogInformation(
                    "{Name} {Symbol} pulled {Count} new or updated open orders",
                    nameof(OrderSynchronizer), symbol, count);
            }
        }
    }
}