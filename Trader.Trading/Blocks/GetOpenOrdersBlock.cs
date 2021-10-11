using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class GetOpenOrdersBlock
    {
        private static string TypeName => nameof(GetOpenOrdersBlock);

        public static ValueTask<ImmutableSortedOrderSet> GetOpenOrdersAsync(this IAlgoContext context, Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return context.GetOpenOrdersInnerAsync(symbol, side, cancellationToken);
        }

        private static async ValueTask<ImmutableSortedOrderSet> GetOpenOrdersInnerAsync(this IAlgoContext context, Symbol symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();

            // todo: refactor this to call a local provider
            var orders = await repository.GetTransientOrdersBySideAsync(symbol.Name, side, cancellationToken).ConfigureAwait(false);

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