using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

namespace Outcompute.Trader.Trading.Commands.AveragingSell
{
    internal partial class AveragingSellExecutor : IAlgoCommandExecutor<AveragingSellCommand>
    {
        private readonly ILogger _logger;

        public AveragingSellExecutor(ILogger<AveragingSellExecutor> logger)
        {
            _logger = logger;
        }

        private static string TypeName => nameof(AveragingSellExecutor);

        public async ValueTask ExecuteAsync(IAlgoContext context, AveragingSellCommand command, CancellationToken cancellationToken = default)
        {
            // calculate the desired sell
            var desired = CalculateDesiredSell(context, command, command.Symbol, command.ProfitMultiplier, command.Orders);

            // apply the desired sell
            if (desired == DesiredSell.None)
            {
                await new CancelOpenOrdersCommand(command.Symbol, OrderSide.Sell)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await new EnsureSingleOrderCommand(command.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, desired.Quantity, desired.Price, command.RedeemSavings, command.RedeemSwapPool)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private DesiredSell CalculateDesiredSell(IAlgoContext context, AveragingSellCommand command, Symbol symbol, decimal profitMultiplier, IReadOnlyCollection<OrderQueryResult> orders)
        {
            // loop the orders only once and calculate all required stats up front
            var quantity = 0M;
            var notional = 0M;
            foreach (var order in orders)
            {
                quantity += order.ExecutedQuantity;
                notional += order.ExecutedQuantity * order.Price;
            }
            var price = notional / quantity;

            // break if there are no assets to sell
            var free = context.BaseAssetSpotBalance.Free
                + (command.RedeemSavings ? context.BaseAssetSavingsBalance.FreeAmount : 0)
                + (command.RedeemSwapPool ? context.BaseAssetSwapPoolBalance.Total : 0);

            if (free < quantity)
            {
                LogCannotEvaluateDesiredSell(TypeName, symbol.Name, quantity, symbol.BaseAsset, free);

                return DesiredSell.None;
            }

            // adjust the quantity down to lot size filter so we have a valid order
            quantity = quantity.AdjustQuantityDownToLotStepSize(context.Symbol);

            // break if the quantity falls below the minimum lot size
            if (quantity < symbol.Filters.LotSize.MinQuantity)
            {
                LogCannotSetSellOrderLotSize(TypeName, symbol.Name, quantity, symbol.BaseAsset, symbol.Filters.LotSize.MinQuantity);

                return DesiredSell.None;
            }

            // bump the price by the profit multipler so we have a minimum sell price
            price *= profitMultiplier;

            // adjust the sell price up to the minimum percent filter
            price = Math.Max(price, context.Ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierDown);

            // adjust the sell price up to the tick size
            price = price.AdjustPriceUpToTickSize(context.Symbol);

            // check if the sell is under the minimum notional filter
            if (quantity * price < symbol.Filters.MinNotional.MinNotional)
            {
                LogCannotSetSellOrderNotional(TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.Filters.MinNotional.MinNotional);

                return DesiredSell.None;
            }

            // check if the sell is above the maximum percent filter
            if (price > context.Ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierUp)
            {
                LogCannotSetSellOrderMaximumPercentFilter(TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, context.Ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierUp);

                return DesiredSell.None;
            }

            // only sell if the price is at or above the ticker
            if (context.Ticker.ClosePrice < price)
            {
                LogHoldingOffSellOrder(TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, price / context.Ticker.ClosePrice, context.Ticker.ClosePrice);

                return DesiredSell.None;
            }

            // otherwise we now have a valid desired sell
            return new DesiredSell(quantity, price);
        }

        private sealed record DesiredSell(decimal Quantity, decimal Price)
        {
            public static readonly DesiredSell None = new(0m, 0m);
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Warning, "{Type} {Name} cannot evaluate sell order of {Quantity:F8} {Asset} because the free quantity is only {Free:F8} {Asset}")]
        private partial void LogCannotEvaluateDesiredSell(string type, string name, decimal quantity, string asset, decimal free);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity:F8} {Asset} because the quantity is under the minimum lot size of {MinLotSize:F8} {Asset}")]
        private partial void LogCannotSetSellOrderLotSize(string type, string name, decimal quantity, string asset, decimal minLotSize);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity:F8} {Asset} at {Price:F8} {Quote} totalling {Total:F8} {Quote} because it is under the minimum notional of {MinNotional:F8} {Quote}")]
        private partial void LogCannotSetSellOrderNotional(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity:F8} {Asset} at {Price:F8} {Quote} totalling {Total:F8} {Quote} because it is under the maximum percent filter price of {MaxPrice:F8} {Quote}")]
        private partial void LogCannotSetSellOrderMaximumPercentFilter(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal maxPrice);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} holding off sell order of {Quantity:F8} {Asset} until price hits {Price:F8} {Quote} ({Percent:P2} of current value of {Ticker:F8} {Quote})")]
        private partial void LogHoldingOffSellOrder(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal percent, decimal ticker);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} topped up quantity of {Quantity:F8} {Asset} with {Required:F8} {Asset} so it meets the minimum lot size of {MinLotSize:F8} {Asset}")]
        private partial void LogToppedUp(string type, string name, decimal quantity, string asset, decimal required, decimal minLotSize);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} could not top up quantity of {Quantity:F8} {Asset} with {Required:F8} {Asset} to meet the minimum lot size of {MinLotSize:F8} {Asset} because the extra free quantity is only {Allowed:F8} {Asset}")]
        private partial void LogCouldNotTopUpToMinLotSize(string type, string name, decimal quantity, string asset, decimal required, decimal minLotSize, decimal allowed);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} could not top up quantity of {Quantity:F8} {Asset} with {Required:F8} {Asset} to meet the minimum notional of {MinNotional:F8} {Quote} because the extra free quantity is only {Allowed:F8} {Asset}")]
        private partial void LogCouldNotTopUpToMinNotional(string type, string name, decimal quantity, string asset, decimal required, decimal minNotional, string quote, decimal allowed);

        #endregion Logging
    }
}