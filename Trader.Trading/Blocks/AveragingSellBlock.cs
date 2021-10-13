using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class AveragingSellBlock
    {
        private static string TypeName => nameof(AveragingSellBlock);

        public static ValueTask<Profit> SetAveragingSellAsync(this IAlgoContext context, Symbol symbol, decimal profitMultiplier, bool redeemSavings, bool sellSavings, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return SetAveragingSellInnerAsync(context, symbol, profitMultiplier, redeemSavings, sellSavings, cancellationToken);
        }

        private static async ValueTask<Profit> SetAveragingSellInnerAsync(IAlgoContext context, Symbol symbol, decimal profitMultiplier, bool redeemSavings, bool sellSavings, CancellationToken cancellationToken)
        {
            // resolve services
            var significantOrderResolver = context.ServiceProvider.GetRequiredService<ISignificantOrderResolver>();
            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();
            var savingsProvider = context.ServiceProvider.GetRequiredService<ISavingsProvider>();
            var tickerProvider = context.ServiceProvider.GetRequiredService<ITickerProvider>();
            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();

            // get any required filters from the symbol
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var percentFilter = symbol.Filters.OfType<PercentPriceSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();

            // get all significant buys
            var significant = await significantOrderResolver.ResolveAsync(symbol, cancellationToken).ConfigureAwait(false);

            // get the current balance
            var balance = await repository.TryGetBalanceAsync(symbol.BaseAsset, cancellationToken).ConfigureAwait(false)
                ?? Balance.Zero(symbol.BaseAsset);

            // get all savings if applicable
            var savings = (sellSavings ? await savingsProvider.TryGetFirstFlexibleProductPositionAsync(symbol.BaseAsset, cancellationToken).ConfigureAwait(false) : null)
                ?? FlexibleProductPosition.Zero(symbol.BaseAsset);

            // get the current ticker for the symbol
            var ticker = await tickerProvider.TryGetTickerAsync(symbol.Name, cancellationToken).ConfigureAwait(false);

            // calculate the desired sell
            var desired = CalculateDesiredSell(logger, symbol, profitMultiplier, significant.Orders, balance, savings, lotSizeFilter, percentFilter, ticker, priceFilter, minNotionalFilter);

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

            // return the latest known profit
            return significant.Profit;
        }

        private static DesiredSell CalculateDesiredSell(ILogger logger, Symbol symbol, decimal profitMultiplier, ImmutableSortedOrderSet orders, Balance balance, FlexibleProductPosition savings, LotSizeSymbolFilter lotSizeFilter, PercentPriceSymbolFilter percentFilter, MiniTicker? ticker, PriceSymbolFilter priceFilter, MinNotionalSymbolFilter minNotionalFilter)
        {
            // skip if there is no ticker information
            if (ticker is null)
            {
                logger.LogWarning(
                    "{Type} cannot evaluate desired sell for symbol {Symbol} because no ticker information is yet available",
                    TypeName, symbol.Name);

                return DesiredSell.None;
            }

            // skip if there is nothing to sell
            if (orders.IsEmpty)
            {
                return DesiredSell.None;
            }

            // take all known significant buy orders on the symbol
            var quantity = orders.Sum(x => x.ExecutedQuantity);

            // break if there is nothing to sell
            if (quantity <= 0m)
            {
                return DesiredSell.None;
            }

            // break if there are no assets to sell
            var total = balance.Free + savings.FreeAmount;
            if (total <= 0m)
            {
                logger.LogWarning(
                    "{Type} cannot evaluate desired sell for symbol {Symbol} because there are not enough assets available to sell",
                    TypeName, symbol.Name);

                return DesiredSell.None;
            }

            // detect the excess quantity available - gained interest etc
            var excess = total - quantity;

            // warn if there negative excess
            // this can happen due to delay with savings info or the use or other products not monitored by the application
            if (excess < 0m)
            {
                logger.LogWarning(
                    "{Type} detected negative excess for symbol {Symbol}",
                    TypeName, symbol.Name);

                excess = 0m;
            }

            // calculate the weighted average price on all the significant orders
            var price = orders.Sum(x => x.Price * x.ExecutedQuantity);

            // if there is excess then add it at near zero cost
            if (excess > 0)
            {
                price += 0.00000001m * excess;
                quantity += excess;
            }

            price /= quantity;

            // bump the price by the profit multipler so we have a sell price
            price *= profitMultiplier;

            // adjust the quantity down to lot size filter
            if (quantity < lotSizeFilter.StepSize)
            {
                logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, lotSizeFilter.StepSize, symbol.BaseAsset);

                return DesiredSell.None;
            }
            quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // adjust the sell price up to the minimum percent filter
            var minPrice = ticker.ClosePrice * percentFilter.MultiplierDown;
            if (price < minPrice)
            {
                price = minPrice;
            }

            // adjust the sell price up to the tick size
            price = Math.Ceiling(price / priceFilter.TickSize) * priceFilter.TickSize;

            // check if the sell is under the minimum notional filter
            if (quantity * price < minNotionalFilter.MinNotional)
            {
                logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, minNotionalFilter.MinNotional, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // check if the sell is above the maximum percent filter
            if (price > ticker.ClosePrice * percentFilter.MultiplierUp)
            {
                logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the maximum percent filter price of {MaxPrice} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, ticker.ClosePrice * percentFilter.MultiplierUp, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // only sell if the price is at or above the ticker
            if (ticker.ClosePrice < price)
            {
                logger.LogInformation(
                    "{Type} {Name} holding off sell order of {Quantity} {Asset} until price hits {Price} {Quote} ({Percent:P2} of current value of {Ticker} {Quote})",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, price / ticker.ClosePrice, ticker.ClosePrice, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // otherwise we now have a valid desired sell
            return new DesiredSell(quantity, price);
        }

        private sealed record DesiredSell(decimal Quantity, decimal Price)
        {
            public static readonly DesiredSell None = new(0m, 0m);
        }
    }
}