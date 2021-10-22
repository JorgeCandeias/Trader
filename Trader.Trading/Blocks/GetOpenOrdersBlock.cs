using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using Outcompute.Trader.Trading.Providers.Trades;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class GetOpenOrdersBlock
    {
        private static string TypeName => nameof(GetOpenOrdersBlock);

        public static ValueTask<IReadOnlyList<OrderQueryResult>> GetOpenOrdersAsync(this IAlgoContext context, Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
            var orderProvider = context.ServiceProvider.GetRequiredService<IOrderProvider>();

            return GetOpenOrdersInnerAsync(symbol, side, logger, orderProvider, cancellationToken);
        }

        private static async ValueTask<IReadOnlyList<OrderQueryResult>> GetOpenOrdersInnerAsync(Symbol symbol, OrderSide side, ILogger logger, IOrderProvider orderProvider, CancellationToken cancellationToken)
        {
            var orders = await orderProvider
                .GetTransientOrdersBySideAsync(symbol.Name, side, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                logger.LogInformation(
                    "{Type} {Name} identified open {OrderSide} {OrderType} order for {Quantity} {Asset} at {Price} {Quote} totalling {Notional:N8} {Quote}",
                    TypeName, symbol.Name, order.Side, order.Type, order.OriginalQuantity, symbol.BaseAsset, order.Price, symbol.QuoteAsset, order.OriginalQuantity * order.Price, symbol.QuoteAsset);
            }

            return orders;
        }
    }
}