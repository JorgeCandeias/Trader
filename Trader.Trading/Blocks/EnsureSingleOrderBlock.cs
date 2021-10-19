using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class EnsureSingleOrderBlock
    {
        private static string TypeName => nameof(EnsureSingleOrderBlock);

        public static ValueTask<bool> EnsureSingleOrderAsync(this IAlgoContext context, Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();
            var savingsOptions = context.ServiceProvider.GetRequiredService<IOptions<SavingsOptions>>().Value;

            return context.EnsureSingleOrderInnerAsync(symbol, side, type, timeInForce, quantity, price, redeemSavings, savingsOptions, logger, cancellationToken);
        }

        private static async ValueTask<bool> EnsureSingleOrderInnerAsync(this IAlgoContext context, Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, SavingsOptions savingsOptions, ILogger logger, CancellationToken cancellationToken = default)
        {
            // get current open orders
            var orders = await context
                .GetOpenOrdersAsync(symbol, side, cancellationToken)
                .ConfigureAwait(false);

            // cancel all non-desired orders
            var count = orders.Count;
            foreach (var order in orders)
            {
                if (order.Type != type || order.OriginalQuantity != quantity || order.Price != price)
                {
                    await context
                        .CancelOrderAsync(order.Symbol, order.OrderId, cancellationToken)
                        .ConfigureAwait(false);

                    count--;
                }
            }

            // if any order survived then we can stop here
            if (count > 0) return true;

            // get the balance for the affected asset
            var sourceAsset = side switch
            {
                OrderSide.Buy => symbol.QuoteAsset,
                OrderSide.Sell => symbol.BaseAsset,
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };
            var balance = await context
                .TryGetBalanceAsync(sourceAsset, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new AlgorithmException($"Could not get balance for asset '{sourceAsset}'");

            // get the quantity for the affected asset
            var sourceQuantity = side switch
            {
                OrderSide.Buy => quantity * price,
                OrderSide.Sell => quantity,
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };

            // if there is not enough units to place the order then attempt to redeem from savings
            if (balance.Free < sourceQuantity)
            {
                if (redeemSavings)
                {
                    var necessary = sourceQuantity - balance.Free;

                    var (success, redeemed) = await context
                        .TryRedeemSavingsAsync(sourceAsset, necessary, cancellationToken)
                        .ConfigureAwait(false);

                    if (success)
                    {
                        logger.LogInformation(
                            "{Type} {Name} redeemed {Redeemed:F8} {Asset} from savings to cover the necessary {Necessary:F8} {Asset} and will wait {Wait} for the operation to complete",
                            TypeName, symbol.Name, redeemed, sourceAsset, necessary, sourceAsset, savingsOptions.SavingsRedemptionDelay);

                        await Task.Delay(savingsOptions.SavingsRedemptionDelay, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogWarning(
                            "{type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from savings",
                            TypeName, symbol.Name, necessary, sourceAsset);

                        return false;
                    }
                }
                else
                {
                    logger.LogWarning(
                        "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity:F8} {Asset} for {Price:F8} {Quote} but there is only {Free:F8} {Asset} available and savings redemption is disabled",
                        TypeName, symbol.Name, type, side, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, balance.Free, sourceAsset);

                    return false;
                }
            }

            // if we got here then we can place the order
            await context
                .CreateOrderAsync(symbol, type, side, timeInForce, quantity, price, $"{symbol.Name}{price:F8}".Replace(".", "", StringComparison.Ordinal), cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
    }
}