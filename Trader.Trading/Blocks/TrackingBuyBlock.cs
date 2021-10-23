using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class TrackingBuyBlock
    {
        private static string TypeName => nameof(TrackingBuyBlock);

        public static ValueTask<bool> SetTrackingBuyAsync(this IAlgoContext context, Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return SetTrackingBuyInnerAsync(context, symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, cancellationToken);
        }

        private static async ValueTask<bool> SetTrackingBuyInnerAsync(IAlgoContext context, Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, CancellationToken cancellationToken)
        {
            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();

            var ticker = await context.GetRequiredTickerAsync(symbol.Name, cancellationToken).ConfigureAwait(false);
            var orders = await context.GetOpenOrdersAsync(symbol, OrderSide.Buy, cancellationToken).ConfigureAwait(false);
            var balance = await context.GetBalanceProvider().GetRequiredBalanceAsync(symbol.QuoteAsset, cancellationToken).ConfigureAwait(false);
            var savings = await context.GetSavingsProvider().GetPositionOrZeroAsync(symbol.QuoteAsset, cancellationToken).ConfigureAwait(false);

            // identify the free balance
            var free = balance.Free + (redeemSavings ? savings.FreeAmount : 0m);

            // identify the target low price for the first buy
            var lowBuyPrice = ticker.ClosePrice * pullbackRatio;

            // under adjust the buy price to the tick size
            lowBuyPrice = Math.Floor(lowBuyPrice / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;

            logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                TypeName, symbol.Name, lowBuyPrice, symbol.QuoteAsset, ticker.ClosePrice, symbol.QuoteAsset);

            orders = await TryCloseLowBuysAsync(context, logger, symbol, orders, lowBuyPrice, cancellationToken).ConfigureAwait(false);

            orders = await TryCloseHighBuysAsync(context, logger, symbol, orders, cancellationToken).ConfigureAwait(false);

            // if there are still open orders then leave them be
            if (orders.Count > 0)
            {
                return false;
            }

            // calculate the target notional
            var total = free * targetQuoteBalanceFractionPerBuy;

            // cap it at the max notional
            if (maxNotional.HasValue)
            {
                total = Math.Min(total, maxNotional.Value);
            }

            // bump it to the minimum notional if needed
            total = Math.Max(total, symbol.Filters.MinNotional.MinNotional);

            // calculate the appropriate quantity to buy
            var quantity = total / lowBuyPrice;

            // round it down to the lot size step
            quantity = Math.Ceiling(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;

            // calculat the true notional after adjustments
            total = quantity * lowBuyPrice;

            // check if it still is under the max notional after adjustments - some assets have very high minimum notionals or lot sizes
            if (maxNotional.HasValue && total > maxNotional)
            {
                logger.LogError(
                    "{Type} {Name} cannot place buy order with amount of {Total} {Quote} because it is above the configured maximum notional of {MaxNotional}",
                    TypeName, symbol.Name, total, symbol.QuoteAsset, maxNotional);

                return false;
            }

            // ensure there is enough quote asset for it
            if (total > free)
            {
                var necessary = total - free;

                logger.LogWarning(
                    "{Type} {Name} must place order with amount of {Total} {Quote} but the free amount is only {Free} {Quote}",
                    TypeName, symbol.Name, total, symbol.QuoteAsset, free, symbol.QuoteAsset);

                if (redeemSavings)
                {
                    logger.LogInformation(
                        "Will attempt to redeem the necessary {Necessary} {Quote} from savings...",
                        necessary, symbol.QuoteAsset);

                    var (success, actual) = await context.TryRedeemSavingsAsync(symbol.QuoteAsset, necessary, cancellationToken).ConfigureAwait(false);
                    if (success)
                    {
                        logger.LogInformation(
                            "{Type} {Name} redeemed {Quantity} {Asset} from savings to cover the necessary {Necessary} {Asset}",
                            TypeName, symbol.Name, actual, symbol.QuoteAsset, necessary, symbol.QuoteAsset);

                        return true;
                    }
                    else
                    {
                        logger.LogError(
                            "{Type} {Name} could not redeem the necessary {Quantity} {Asset} from savings",
                            TypeName, symbol.Name, necessary, symbol.QuoteAsset);

                        return false;
                    }
                }
            }

            logger.LogInformation(
                "{Type} {Name} placing {OrderType} {OrderSode} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                TypeName, symbol.Name, OrderType.Limit, OrderSide.Buy, symbol.Name, quantity, symbol.BaseAsset, lowBuyPrice, symbol.QuoteAsset, quantity * lowBuyPrice, symbol.QuoteAsset);

            // place the order now
            var tag = $"{symbol.Name}{lowBuyPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
            await context
                .CreateOrderAsync(symbol, OrderType.Limit, OrderSide.Buy, TimeInForce.GoodTillCanceled, quantity, lowBuyPrice, tag, cancellationToken)
                .ConfigureAwait(false);

            return true;
        }

        private static async Task<IReadOnlyList<OrderQueryResult>> TryCloseLowBuysAsync(IAlgoContext context, ILogger logger, Symbol symbol, IReadOnlyList<OrderQueryResult> orders, decimal lowBuyPrice, CancellationToken cancellationToken)
        {
            // cancel all open buy orders with an open price lower than the lower band to the current price
            foreach (var order in orders.Where(x => x.Side == OrderSide.Buy && x.Price < lowBuyPrice))
            {
                logger.LogInformation(
                    "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                    TypeName, symbol.Name, order.Price, order.OriginalQuantity);

                await context
                    .CancelOrderAsync(symbol.Name, order.OrderId, cancellationToken)
                    .ConfigureAwait(false);

                orders = orders.ToImmutableList().Remove(order);
            }

            return orders;
        }

        private static async Task<IReadOnlyList<OrderQueryResult>> TryCloseHighBuysAsync(IAlgoContext context, ILogger logger, Symbol symbol, IReadOnlyList<OrderQueryResult> orders, CancellationToken cancellationToken)
        {
            foreach (var order in orders.Where(x => x.Side == OrderSide.Buy).OrderBy(x => x.Price).Skip(1))
            {
                logger.LogInformation(
                    "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                    TypeName, symbol.Name, order.Price, order.OriginalQuantity);

                await context
                    .CancelOrderAsync(symbol.Name, order.OrderId, cancellationToken)
                    .ConfigureAwait(false);

                orders = orders.ToImmutableList().Remove(order);
            }

            // let the algo resync if any orders where closed
            return orders;
        }
    }
}