using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using Outcompute.Trader.Trading.Providers.Trades;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ClearOpenOrdersBlock
    {
        public static ValueTask ClearOpenOrdersAsync(this IAlgoContext context, Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var trader = context.ServiceProvider.GetRequiredService<ITradingService>();
            var orderProvider = context.ServiceProvider.GetRequiredService<IOrderProvider>();

            return ClearOpenOrdersCoreAsync(symbol, side, trader, orderProvider, cancellationToken);
        }

        private static async ValueTask ClearOpenOrdersCoreAsync(Symbol symbol, OrderSide side, ITradingService trader, IOrderProvider orderProvider, CancellationToken cancellationToken)
        {
            var orders = await orderProvider
                .GetTransientOrdersBySideAsync(symbol.Name, side, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                var result = await trader
                    .CancelOrderAsync(symbol.Name, order.OrderId, cancellationToken)
                    .ConfigureAwait(false);

                await orderProvider
                    .SetOrderAsync(result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}