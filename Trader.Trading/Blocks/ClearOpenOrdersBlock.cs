using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
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

            return ClearOpenOrdersInnerAsync(context, symbol, side, cancellationToken);
        }

        private static async ValueTask ClearOpenOrdersInnerAsync(IAlgoContext context, Symbol symbol, OrderSide side, CancellationToken cancellationToken)
        {
            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();
            var trader = context.ServiceProvider.GetRequiredService<ITradingService>();
            var clock = context.ServiceProvider.GetRequiredService<ISystemClock>();

            var orders = await repository.GetTransientOrdersBySideAsync(symbol.Name, side, cancellationToken).ConfigureAwait(false);

            foreach (var order in orders)
            {
                var result = await trader
                    .CancelOrderAsync(new CancelStandardOrder(symbol.Name, order.OrderId, null, null, null, clock.UtcNow), cancellationToken)
                    .ConfigureAwait(false);

                await repository
                    .SetOrderAsync(result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}