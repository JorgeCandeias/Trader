using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public static class AveragingSellBlock
    {
        private static string TypeName => nameof(AveragingSellBlock);

        public static ValueTask SetAveragingSellAsync(this IAlgoContext context, Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            foreach (var order in orders)
            {
                if (order.Side != OrderSide.Buy)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Order {order.OrderId} is not a buy order");
                }
                else if (order.ExecutedQuantity <= 0m)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Order {order.OrderId} has non-significant executed quantity");
                }
            }

            return SetAveragingSellInnerAsync(context, symbol, orders, profitMultiplier, redeemSavings, cancellationToken);
        }

        private static async ValueTask SetAveragingSellInnerAsync(IAlgoContext context, Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, CancellationToken cancellationToken)
        {
            // resolve services
            var repository = context.ServiceProvider.GetRequiredService<ITradingRepository>();
            var savingsProvider = context.ServiceProvider.GetRequiredService<ISavingsProvider>();
            var tickerProvider = context.ServiceProvider.GetRequiredService<ITickerProvider>();
            var logger = context.ServiceProvider.GetRequiredService<ILogger<IAlgoContext>>();

            // get the current balance
            var balance = await repository.TryGetBalanceAsync(symbol.BaseAsset, cancellationToken).ConfigureAwait(false)
                ?? Balance.Zero(symbol.BaseAsset);

            // get all savings if applicable
            var savings = (redeemSavings ? await savingsProvider.TryGetFirstFlexibleProductPositionAsync(symbol.BaseAsset, cancellationToken).ConfigureAwait(false) : null)
                ?? FlexibleProductPosition.Zero(symbol.BaseAsset);

            // get the current ticker for the symbol
            var ticker = await tickerProvider.TryGetTickerAsync(symbol.Name, cancellationToken).ConfigureAwait(false);

            // calculate the desired sell
            var desired = CalculateDesiredSell(logger, symbol, profitMultiplier, orders, balance, savings, ticker);

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
                    .EnsureSingleOrderAsync(symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, desired.Quantity, desired.Price, redeemSavings, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static DesiredSell CalculateDesiredSell(ILogger logger, Symbol symbol, decimal profitMultiplier, IReadOnlyCollection<OrderQueryResult> orders, Balance balance, FlexibleProductPosition savings, MiniTicker? ticker)
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
            if (orders.Count == 0)
            {
                return DesiredSell.None;
            }

            // take all known significant buy orders on the symbol
            var quantity = orders.Sum(x => x.ExecutedQuantity);

            // break if there are no assets to sell
            var total = balance.Free + savings.FreeAmount;
            if (total < quantity)
            {
                logger.LogWarning(
                    "{Type} cannot evaluate desired sell for symbol {Symbol} because there are not enough assets available to sell",
                    TypeName, symbol.Name);

                return DesiredSell.None;
            }

            // calculate the weighted average price on all the significant orders
            var price = orders.Sum(x => x.Price * x.ExecutedQuantity) / quantity;

            // bump the price by the profit multipler so we have a sell price
            price *= profitMultiplier;

            // adjust the quantity down to lot size filter
            if (quantity < symbol.Filters.LotSize.StepSize)
            {
                logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, symbol.Filters.LotSize.StepSize, symbol.BaseAsset);

                return DesiredSell.None;
            }
            quantity = Math.Floor(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;

            // adjust the sell price up to the minimum percent filter
            var minPrice = ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierDown;
            if (price < minPrice)
            {
                price = minPrice;
            }

            // adjust the sell price up to the tick size
            price = Math.Ceiling(price / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;

            // check if the sell is under the minimum notional filter
            if (quantity * price < symbol.Filters.MinNotional.MinNotional)
            {
                logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, symbol.Filters.MinNotional.MinNotional, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // check if the sell is above the maximum percent filter
            if (price > ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierUp)
            {
                logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the maximum percent filter price of {MaxPrice} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierUp, symbol.QuoteAsset);

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