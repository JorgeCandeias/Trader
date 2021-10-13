using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class SignificantAveragingSellBlock
    {
        private static string TypeName => nameof(SignificantAveragingSellBlock);

        public static ValueTask SetSignificantAveragingSellAsync(this IAlgoContext context, Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));
            if (ticker is null) throw new ArgumentNullException(nameof(ticker));
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            foreach (var order in orders)
            {
                if (order.Side != OrderSide.Buy)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Parameter '{nameof(orders)}' must only contain orders with side '{OrderSide.Buy}'");
                }
                else if (order.ExecutedQuantity <= 0m)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Parameter '{nameof(orders)}' must only contain orders with executed quantity greater than zero'");
                }
            }

            return SetSignificantAveragingSellInnerAsync(context, symbol, ticker, orders, minimumProfitRate, redeemSavings, cancellationToken);
        }

        private static async ValueTask SetSignificantAveragingSellInnerAsync(IAlgoContext context, Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, CancellationToken cancellationToken)
        {
            // get any required filters from the symbol
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();

            // calculate the desired sell
            var desired = CalculateDesiredSell(context, symbol, minimumProfitRate, orders, lotSizeFilter, priceFilter, ticker, minNotionalFilter);

            // apply the desired sell
            if (desired == DesiredSell.None)
            {
                await context
                    .ClearOpenOrdersAsync(symbol, OrderSide.Sell, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await context
                    .EnsureSingleOrderAsync(symbol, OrderSide.Sell, OrderType.Limit, desired.Quantity, desired.Price, redeemSavings, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static DesiredSell CalculateDesiredSell(IAlgoContext context, Symbol symbol, decimal minimumProfitRate, IReadOnlyCollection<OrderQueryResult> orders, LotSizeSymbolFilter lotSizeFilter, PriceSymbolFilter priceFilter, MiniTicker ticker, MinNotionalSymbolFilter minNotionalFilter)
        {
            // skip if there is nothing to sell
            if (orders.Count == 0)
            {
                return DesiredSell.None;
            }

            // elect lowest significant orders that fit under the minimum profit rate when sold
            var count = 0;
            var numerator = 0m;
            var quantity = 0m;

            foreach (var order in orders.OrderBy(x => x.Price))
            {
                // calculate the candidate average sell price
                var orderNumerator = order.ExecutedQuantity * order.Price;
                var orderQuantity = order.ExecutedQuantity;
                var candidateNumerator = numerator + orderNumerator;
                var candidateQuantity = quantity + orderQuantity;
                var candidateAverageBuyPrice = candidateNumerator / candidateQuantity;
                var candidateSellPrice = candidateAverageBuyPrice * minimumProfitRate;

                // adjust the candidate average sell price up to the tick size
                candidateSellPrice = Math.Ceiling(candidateSellPrice / priceFilter.TickSize) * priceFilter.TickSize;

                // elect the order if the candidate average sell price is below the ticker
                if (candidateSellPrice <= ticker.ClosePrice)
                {
                    count++;
                    numerator = candidateNumerator;
                    quantity = candidateQuantity;
                }
                else
                {
                    break;
                }
            }

            // skip if no buy orders were elected for selling
            if (count <= 0)
            {
                context.GetLogger().LogInformation(
                    "{Type} {Symbol} cannot elect any buy orders for selling at a minimum profit rate of {MinimumProfitRate}",
                    TypeName, symbol.Name, minimumProfitRate);

                return DesiredSell.None;
            }

            // break if the quantity is under the minimum lot size
            if (quantity < lotSizeFilter.StepSize)
            {
                context.GetLogger().LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, lotSizeFilter.StepSize, symbol.BaseAsset);

                return DesiredSell.None;
            }

            // adjust the quantity down to the lot size filter
            quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // check if the sell is under the minimum notional filter
            if (quantity * ticker.ClosePrice < minNotionalFilter.MinNotional)
            {
                context.GetLogger().LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, ticker.ClosePrice, symbol.QuoteAsset, quantity * ticker.ClosePrice, symbol.QuoteAsset, minNotionalFilter.MinNotional, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // otherwise we now have a valid desired sell
            return new DesiredSell(quantity, ticker.ClosePrice);
        }

        private sealed record DesiredSell(decimal Quantity, decimal Price)
        {
            public static readonly DesiredSell None = new(0m, 0m);
        }
    }
}