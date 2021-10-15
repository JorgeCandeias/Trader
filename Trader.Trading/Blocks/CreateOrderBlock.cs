using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class CreateOrderBlock
    {
        private static string TypeName => nameof(CreateOrderBlock);

        public static ValueTask<OrderResult> CreateOrderAsync(this IAlgoContext context, Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
            var trader = context.ServiceProvider.GetRequiredService<ITradingService>();
            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();

            return CreateOrderInnerAsync(symbol, type, side, timeInForce, quantity, price, tag, logger, trader, repository, cancellationToken);
        }

        private static async ValueTask<OrderResult> CreateOrderInnerAsync(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal quantity, decimal price, string? tag, ILogger logger, ITradingService trader, ITradingRepository repository, CancellationToken cancellationToken = default)
        {
            // if we got here then we can place the order
            var watch = Stopwatch.StartNew();

            logger.LogInformation(
                "{Type} {Name} placing {OrderType} {OrderSide} order for {Quantity:F8} {Asset} at {Price:F8} {Quote} for a total of {Total:F8} {Quote}",
                TypeName, symbol.Name, type, side, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset);

            var result = await trader
                .CreateOrderAsync(symbol.Name, side, type, timeInForce, quantity, null, price, tag, null, null, cancellationToken)
                .ConfigureAwait(false);

            await repository
                .SetOrderAsync(result, 0m, 0m, 0m, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "{Type} {Name} placed {OrderType} {OrderSide} order for {Quantity:F8} {Asset} at {Price:F8} {Quote} for a total of {Total:F8} {Quote} in {ElapsedMs}ms",
                TypeName, symbol.Name, type, side, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, watch.ElapsedMilliseconds);

            return result;
        }
    }
}