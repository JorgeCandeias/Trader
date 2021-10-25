using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.ClearOpenOrders
{
    internal class ClearOpenOrdersService : IClearOpenOrdersService
    {
        private readonly ITradingService _trader;
        private readonly IOrderProvider _orders;

        public ClearOpenOrdersService(ITradingService trader, IOrderProvider orders)
        {
            _trader = trader;
            _orders = orders;
        }

        public Task ClearOpenOrdersAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return ClearOpenOrdersCoreAsync(symbol, side, cancellationToken);
        }

        private async Task ClearOpenOrdersCoreAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken)
        {
            var orders = await _orders
                .GetTransientOrdersBySideAsync(symbol.Name, side, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                var result = await _trader
                    .CancelOrderAsync(symbol.Name, order.OrderId, cancellationToken)
                    .ConfigureAwait(false);

                await _orders
                    .SetOrderAsync(result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}