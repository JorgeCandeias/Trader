using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

namespace Outcompute.Trader.Trading.Commands.SignificantAveragingSell;

internal partial class SignificantAveragingSellExecutor : IAlgoCommandExecutor<SignificantAveragingSellCommand>
{
    private readonly ILogger _logger;

    public SignificantAveragingSellExecutor(ILogger<SignificantAveragingSellExecutor> logger)
    {
        _logger = logger;
    }

    private const string TypeName = nameof(SignificantAveragingSellExecutor);

    public ValueTask ExecuteAsync(IAlgoContext context, SignificantAveragingSellCommand command, CancellationToken cancellationToken = default)
    {
        // calculate the desired sell
        var desired = CalculateDesiredSell(context, command);

        // apply the desired sell
        if (desired == DesiredSell.None)
        {
            return new CancelOpenOrdersCommand(command.Symbol, OrderSide.Sell, 0.01M)
                .ExecuteAsync(context, cancellationToken);
        }
        else
        {
            return new EnsureSingleOrderCommand(command.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, desired.Quantity, desired.Price, command.RedeemSavings, command.RedeemSwapPool)
                .ExecuteAsync(context, cancellationToken);
        }
    }

    private DesiredSell CalculateDesiredSell(IAlgoContext context, SignificantAveragingSellCommand command)
    {
        // get context info for the command symbol
        var positions = context.PositionDetailsLookup[command.Symbol.Name].Orders;
        var ticker = context.Tickers[command.Symbol.Name];
        var spots = context.SpotBalances[command.Symbol.Name];
        var savings = context.Savings[command.Symbol.Name];

        // skip if there is nothing to sell
        if (positions.Count == 0)
        {
            return DesiredSell.None;
        }

        // calculate total free from all valid sources
        // this may be less than the target sell due to swap pool fluctuations etc
        var free = spots.BaseAsset.Free
            + (command.RedeemSavings ? savings.BaseAsset.FreeAmount : 0)
            + (command.RedeemSwapPool ? context.BaseAssetSwapPoolBalance.Total : 0);

        // first pass - calculate partials for the entire data
        var count = 0;
        var numerator = 0M;
        var quantity = 0M;
        foreach (var position in positions)
        {
            numerator += position.Price * position.ExecutedQuantity;
            quantity += position.ExecutedQuantity;
            count++;
        }

        // shortcut - see if we can sell everything
        var averagePrice = numerator / quantity;
        var sellablePrice = averagePrice * command.MinimumProfitRate;
        if (!(ticker.ClosePrice >= sellablePrice && quantity <= free))
        {
            // second pass - find sellable group
            foreach (var position in positions)
            {
                // remove this order from the trailing average
                numerator -= position.Price * position.ExecutedQuantity;
                quantity -= position.ExecutedQuantity;
                count--;

                // if this was the last order then give up
                if (count == 0)
                {
                    break;
                }

                // see if the trailing average is sellable
                averagePrice = numerator / quantity;
                sellablePrice = averagePrice * command.MinimumProfitRate;
                if (ticker.ClosePrice >= sellablePrice && quantity <= free)
                {
                    break;
                }
            }
        }

        // skip if no buy orders were elected for selling
        if (count <= 0)
        {
            LogCannotElectedBuyOrders(TypeName, command.Symbol.Name, command.MinimumProfitRate);

            return DesiredSell.None;
        }

        // log details on the orders elected
        foreach (var position in positions.TakeLast(count))
        {
            LogElectedOrder(TypeName, command.Symbol.Name, position.OrderId, position.ExecutedQuantity, command.Symbol.BaseAsset, position.Price, command.Symbol.QuoteAsset);
        }
        LogElectedOrders(TypeName, command.Symbol.Name, count, quantity, command.Symbol.BaseAsset, averagePrice, command.Symbol.QuoteAsset);

        // adjust the quantity down to the lot size filter
        quantity = quantity.AdjustQuantityDownToLotStepSize(command.Symbol);
        LogAdjustedQuantityByLotStepSize(TypeName, command.Symbol.Name, command.Symbol.Filters.LotSize.StepSize, command.Symbol.BaseAsset, quantity);

        // break if the quantity is under the minimum lot size
        if (quantity < command.Symbol.Filters.LotSize.MinQuantity)
        {
            LogCannotSetSellOrder(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, command.Symbol.Filters.LotSize.MinQuantity);

            return DesiredSell.None;
        }

        // calculate the sell notional
        var total = quantity * ticker.ClosePrice;

        LogCalculatedNotional(TypeName, command.Symbol.Name, total, command.Symbol.QuoteAsset, quantity, command.Symbol.BaseAsset, ticker.ClosePrice);

        // check if the sell is under the minimum notional filter
        if (total < command.Symbol.Filters.MinNotional.MinNotional)
        {
            LogCannotSetSellOrderUnderMinimumNotional(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, ticker.ClosePrice, command.Symbol.QuoteAsset, quantity * ticker.ClosePrice, command.Symbol.Filters.MinNotional.MinNotional);

            return DesiredSell.None;
        }

        // otherwise we now have a valid desired sell
        return new DesiredSell(quantity, ticker.ClosePrice);
    }

    private record struct DesiredSell(decimal Quantity, decimal Price)
    {
        public static readonly DesiredSell None = new(0m, 0m);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} {Name} elected order {OrderId} for sale with significant quantity {Quantity:F8} {Asset} at buy price {Price:F8} {Quote}")]
    private partial void LogElectedOrder(string type, string name, long orderId, decimal quantity, string asset, decimal price, string quote);

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} cannot elect any buy orders for selling at a minimum profit rate of {MinimumProfitRate:F8}")]
    private partial void LogCannotElectedBuyOrders(string type, string name, decimal minimumProfitRate);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} elected {Count} orders for sale with total quantity {Quantity:F8} {Asset} at average buy price {Price:F8} {Quote}")]
    private partial void LogElectedOrders(string type, string name, int count, decimal quantity, string asset, decimal price, string quote);

    [LoggerMessage(3, LogLevel.Information, "{Type} {Name} adjusted quantity by lot step size of {LotStepSize} {Asset} down to {Quantity:F8} {Asset}")]
    private partial void LogAdjustedQuantityByLotStepSize(string type, string name, decimal lotStepSize, string asset, decimal quantity);

    [LoggerMessage(4, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}")]
    private partial void LogCannotSetSellOrder(string type, string name, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(5, LogLevel.Information, "{Type} {Name} calculated notional of {Total:F8} {Quote} using quantity of {Quantity:F8} {Asset} and ticker of {Price:F8} {Quote}")]
    private partial void LogCalculatedNotional(string type, string name, decimal total, string quote, decimal quantity, string asset, decimal price);

    [LoggerMessage(6, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}")]
    private partial void LogCannotSetSellOrderUnderMinimumNotional(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

    #endregion Logging
}