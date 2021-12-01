using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CreateOrder;

namespace Outcompute.Trader.Trading.Commands.MarketSell;

internal partial class MarketSellCommandExecutor : IAlgoCommandExecutor<MarketSellCommand>
{
    private readonly ILogger _logger;

    public MarketSellCommandExecutor(ILogger<MarketSellCommandExecutor> logger)
    {
        _logger = logger;
    }

    private const string TypeName = nameof(MarketSellCommandExecutor);
    private const OrderType MyOrderType = OrderType.Market;
    private const OrderSide MyOrderSide = OrderSide.Sell;

    public ValueTask ExecuteAsync(IAlgoContext context, MarketSellCommand command, CancellationToken cancellationToken = default)
    {
        // get context data for the command symbol
        var data = context.Data[command.Symbol.Name];
        var ticker = data.Ticker;
        var orders = data.Orders;

        // ensure there are no other open market orders waiting for execution
        if (orders.Open.Any(x => x.Type == OrderType.Market))
        {
            LogConcurrentMarketOrders(TypeName, context.Name, orders.Open.Where(x => x.Type == OrderType.Market));
            return ValueTask.CompletedTask;
        }

        // adjust the quantity down by the step size to make a valid order
        var quantity = command.Quantity.AdjustQuantityDownToLotStepSize(command.Symbol);
        LogAdjustedQuantity(TypeName, command.Symbol.Name, command.Quantity, command.Symbol.BaseAsset, quantity, command.Symbol.Filters.LotSize.StepSize);

        // if the quantity becomes lower than the minimum lot size then we cant sell
        if (quantity < command.Symbol.Filters.LotSize.MinQuantity)
        {
            LogQuantityLessThanMinLotSize(TypeName, command.Symbol.Name, MyOrderType, MyOrderSide, quantity, command.Symbol.BaseAsset, command.Symbol.Filters.LotSize.MinQuantity);
            return ValueTask.CompletedTask;
        }

        // if the total becomes lower than the minimum notional then we cant sell
        var total = quantity * ticker.ClosePrice;
        if (total < command.Symbol.Filters.MinNotional.MinNotional)
        {
            LogTotalLessThanMinNotional(TypeName, command.Symbol.Name, MyOrderType, MyOrderSide, quantity, command.Symbol.BaseAsset, ticker.ClosePrice, command.Symbol.QuoteAsset, total, command.Symbol.Filters.MinNotional.MinNotional);
            return ValueTask.CompletedTask;
        }

        // all set
        return new CreateOrderCommand(command.Symbol, OrderType.Market, OrderSide.Sell, null, quantity, null, null, null, command.Tag)
            .ExecuteAsync(context, cancellationToken);
    }

    [LoggerMessage(0, LogLevel.Information, "{Type} {Name} adjusted original quantity of {Quantity} {Asset} down to {AdjustedQuantity} {Asset} by step size {StepSize} {Asset}")]
    private partial void LogAdjustedQuantity(string type, string name, decimal quantity, string asset, decimal adjustedQuantity, decimal stepSize);

    [LoggerMessage(1, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} because it is less than the minimum lot size of {MinLotSize} {Asset}")]
    private partial void LogQuantityLessThanMinLotSize(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(2, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} and price {Price} {Quote} because the total of {Total} {Quote} is less than the minimum notional of {MinNotional} {Quote}")]
    private partial void LogTotalLessThanMinNotional(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

    [LoggerMessage(3, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} because the free amount from all sources is only {Free} {Asset}")]
    private partial void LogNotEnoughFreeBalance(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal free);

    [LoggerMessage(4, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from savings")]
    private partial void LogRedeemingSavings(string type, string name, decimal quantity, string asset);

    [LoggerMessage(5, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from the swap pool")]
    private partial void LogRedeemingSwapPool(string type, string name, decimal quantity, string asset);

    [LoggerMessage(6, LogLevel.Error, "{Type} {Name} could not redeem the required {Quantity} {Asset}")]
    private partial void LogCouldNotRedeem(string type, string name, decimal quantity, string asset);

    [LoggerMessage(7, LogLevel.Error, "{Type} {Name} will not execute as there are open market orders being executed: {Orders}")]
    private partial void LogConcurrentMarketOrders(string type, string name, IEnumerable<OrderQueryResult> orders);
}