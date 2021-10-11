using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class ClearOpenBuyOrdersBlock
    {
        public static ValueTask ClearOpenBuyOrdersAsync(this IAlgoContext context, Symbol symbol, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return ClearOpenBuyOrdersInnerAsync(context, symbol, cancellationToken);
        }

        private static async ValueTask ClearOpenBuyOrdersInnerAsync(IAlgoContext context, Symbol symbol, CancellationToken cancellationToken)
        {
            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();
            var trader = context.ServiceProvider.GetRequiredService<ITradingService>();
            var clock = context.ServiceProvider.GetRequiredService<ISystemClock>();

            var orders = await repository.GetTransientOrdersBySideAsync(symbol.Name, OrderSide.Buy, cancellationToken).ConfigureAwait(false);

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