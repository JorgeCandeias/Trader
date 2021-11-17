using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

namespace Outcompute.Trader.Trading.Commands.AveragingSell;

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
        var desired = CalculateDesiredSell(context, command);

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

    private DesiredSell CalculateDesiredSell(IAlgoContext context, AveragingSellCommand command)
    {
        // get context data for the command symbol
        var positions = context.PositionDetailsLookup[command.Symbol.Name].Orders;
        var spots = context.SpotBalances[command.Symbol.Name];
        var savings = context.Savings[command.Symbol.Name];
        var ticker = context.Tickers[command.Symbol.Name];

        // loop the orders only once and calculate all required stats up front
        var quantity = 0M;
        var notional = 0M;
        foreach (var position in positions)
        {
            quantity += position.ExecutedQuantity;
            notional += position.ExecutedQuantity * position.Price;
        }
        var price = notional / quantity;

        // break if there are no assets to sell
        var free = spots.BaseAsset.Free
            + (command.RedeemSavings ? savings.BaseAsset.FreeAmount : 0)
            + (command.RedeemSwapPool ? context.BaseAssetSwapPoolBalance.Total : 0);

        if (free < quantity)
        {
            LogCannotEvaluateDesiredSell(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, free);

            return DesiredSell.None;
        }

        // adjust the quantity down to lot size filter so we have a valid order
        quantity = quantity.AdjustQuantityDownToLotStepSize(command.Symbol);

        // break if the quantity falls below the minimum lot size
        if (quantity < command.Symbol.Filters.LotSize.MinQuantity)
        {
            LogCannotSetSellOrderLotSize(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, command.Symbol.Filters.LotSize.MinQuantity);

            return DesiredSell.None;
        }

        // bump the price by the profit multipler so we have a minimum sell price
        price *= command.ProfitMultiplier;

        // adjust the sell price up to the minimum percent filter
        price = Math.Max(price, ticker.ClosePrice * command.Symbol.Filters.PercentPrice.MultiplierDown);

        // adjust the sell price up to the tick size
        price = price.AdjustPriceUpToTickSize(command.Symbol);

        // check if the sell is under the minimum notional filter
        if (quantity * price < command.Symbol.Filters.MinNotional.MinNotional)
        {
            LogCannotSetSellOrderNotional(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, price, command.Symbol.QuoteAsset, quantity * price, command.Symbol.Filters.MinNotional.MinNotional);

            return DesiredSell.None;
        }

        // check if the sell is above the maximum percent filter
        if (price > ticker.ClosePrice * command.Symbol.Filters.PercentPrice.MultiplierUp)
        {
            LogCannotSetSellOrderMaximumPercentFilter(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, price, command.Symbol.QuoteAsset, quantity * price, context.Ticker.ClosePrice * command.Symbol.Filters.PercentPrice.MultiplierUp);

            return DesiredSell.None;
        }

        // only sell if the price is at or above the ticker
        if (ticker.ClosePrice < price)
        {
            LogHoldingOffSellOrder(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, price, command.Symbol.QuoteAsset, price / ticker.ClosePrice, context.Ticker.ClosePrice);

            return DesiredSell.None;
        }

        // otherwise we now have a valid desired sell
        return new DesiredSell(quantity, price);
    }

    private record struct DesiredSell(decimal Quantity, decimal Price)
    {
        public static readonly DesiredSell None = new(0m, 0m);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Warning, "{Type} {Name} cannot evaluate sell order of {Quantity:F8} {Asset} because the free quantity is only {Free:F8} {Asset}")]
    private partial void LogCannotEvaluateDesiredSell(string type, string name, decimal quantity, string asset, decimal free);

    [LoggerMessage(1, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity:F8} {Asset} because the quantity is under the minimum lot size of {MinLotSize:F8} {Asset}")]
    private partial void LogCannotSetSellOrderLotSize(string type, string name, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(2, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity:F8} {Asset} at {Price:F8} {Quote} totalling {Total:F8} {Quote} because it is under the minimum notional of {MinNotional:F8} {Quote}")]
    private partial void LogCannotSetSellOrderNotional(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

    [LoggerMessage(3, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity:F8} {Asset} at {Price:F8} {Quote} totalling {Total:F8} {Quote} because it is under the maximum percent filter price of {MaxPrice:F8} {Quote}")]
    private partial void LogCannotSetSellOrderMaximumPercentFilter(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal maxPrice);

    [LoggerMessage(4, LogLevel.Information, "{Type} {Name} holding off sell order of {Quantity:F8} {Asset} until price hits {Price:F8} {Quote} ({Percent:P2} of current value of {Ticker:F8} {Quote})")]
    private partial void LogHoldingOffSellOrder(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal percent, decimal ticker);

    [LoggerMessage(5, LogLevel.Information, "{Type} {Name} topped up quantity of {Quantity:F8} {Asset} with {Required:F8} {Asset} so it meets the minimum lot size of {MinLotSize:F8} {Asset}")]
    private partial void LogToppedUp(string type, string name, decimal quantity, string asset, decimal required, decimal minLotSize);

    [LoggerMessage(6, LogLevel.Error, "{Type} {Name} could not top up quantity of {Quantity:F8} {Asset} with {Required:F8} {Asset} to meet the minimum lot size of {MinLotSize:F8} {Asset} because the extra free quantity is only {Allowed:F8} {Asset}")]
    private partial void LogCouldNotTopUpToMinLotSize(string type, string name, decimal quantity, string asset, decimal required, decimal minLotSize, decimal allowed);

    [LoggerMessage(7, LogLevel.Error, "{Type} {Name} could not top up quantity of {Quantity:F8} {Asset} with {Required:F8} {Asset} to meet the minimum notional of {MinNotional:F8} {Quote} because the extra free quantity is only {Allowed:F8} {Asset}")]
    private partial void LogCouldNotTopUpToMinNotional(string type, string name, decimal quantity, string asset, decimal required, decimal minNotional, string quote, decimal allowed);

    #endregion Logging
}