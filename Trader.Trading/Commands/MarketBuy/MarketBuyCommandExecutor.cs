using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CreateOrder;

namespace Outcompute.Trader.Trading.Commands.MarketBuy;

internal partial class MarketBuyCommandExecutor : IAlgoCommandExecutor<MarketBuyCommand>
{
    private readonly ILogger _logger;

    public MarketBuyCommandExecutor(ILogger<MarketBuyCommandExecutor> logger)
    {
        _logger = logger;
    }

    private const string TypeName = nameof(MarketBuyCommandExecutor);

    public ValueTask ExecuteAsync(IAlgoContext context, MarketBuyCommand command, CancellationToken cancellationToken = default)
    {
        // get context data for the command symbol
        var data = context.Data[command.Symbol.Name];
        var orders = data.Orders;

        // ensure there are no other open market orders waiting for execution
        if (orders.Open.Any(x => x.Type == OrderType.Market))
        {
            LogConcurrentMarketOrders(TypeName, context.Name, orders.Open.Where(x => x.Type == OrderType.Market));
            return ValueTask.CompletedTask;
        }

        // raise the quantity if required
        var quantity = command.Quantity;
        if (quantity.HasValue)
        {
            if (command.RaiseToMin)
            {
                quantity = command.Symbol.RaiseQuantityToMinLotSize(quantity.Value);
            }

            if (command.RaiseToStepSize)
            {
                quantity = command.Symbol.RaiseQuantityToLotStepSize(quantity.Value);
            }
        }

        // raise the notional if required
        var notional = command.Notional;
        if (notional.HasValue)
        {
            if (command.RaiseToMin)
            {
                notional = command.Symbol.RaiseTotalUpToMinNotional(notional.Value);
            }

            if (command.RaiseToStepSize)
            {
                notional = SymbolMath.RaisePriceToTickSize(command.Symbol, notional.Value);
            }
        }

        // all set
        return new CreateOrderCommand(command.Symbol, OrderType.Market, OrderSide.Buy, null, quantity, notional, null, null, command.Tag)
            .ExecuteAsync(context, cancellationToken);
    }

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} adjusted quantity of {Quantity} {Asset} up to {AdjustedQuantity} {Asset} by step size {StepSize} {Asset}")]
    private partial void LogAdjustedQuantity(string type, string name, decimal quantity, string asset, decimal adjustedQuantity, decimal stepSize);

    [LoggerMessage(2, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} because it is less than the minimum lot size of {MinLotSize} {Asset}")]
    private partial void LogQuantityLessThanMinLotSize(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal minLotSize);

    [LoggerMessage(3, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} and price {Price} {Quote} because the total of {Total} {Quote} is less than the minimum notional of {MinNotional} {Quote}")]
    private partial void LogTotalLessThanMinNotional(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

    [LoggerMessage(4, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} because the free amount from all sources is only {Free} {Asset}")]
    private partial void LogNotEnoughFreeBalance(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal free);

    [LoggerMessage(5, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from savings")]
    private partial void LogRedeemingSavings(string type, string name, decimal quantity, string asset);

    [LoggerMessage(6, LogLevel.Information, "{Type} {Name} redeemed {Quantity} {Asset} from savings")]
    private partial void LogRedeemedSavings(string type, string name, decimal quantity, string asset);

    [LoggerMessage(7, LogLevel.Error, "{Type} {Name} failed to redeem {Quantity} {Asset} from savings")]
    private partial void LogFailedToRedeemSavings(string type, string name, decimal quantity, string asset);

    [LoggerMessage(8, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from the swap pool")]
    private partial void LogRedeemingSwapPool(string type, string name, decimal quantity, string asset);

    [LoggerMessage(9, LogLevel.Information, "{Type} {Name} redeemed {Quantity} {Asset} from the swap pool")]
    private partial void LogRedeemedSwapPool(string type, string name, decimal quantity, string asset);

    [LoggerMessage(10, LogLevel.Error, "{Type} {Name} failed to redeem {Quantity} {Asset} from the swap pool")]
    private partial void LogFailedToRedeemSwapPool(string type, string name, decimal quantity, string asset);

    [LoggerMessage(11, LogLevel.Error, "{Type} {Name} could not redeem the required {Quantity} {Asset}")]
    private partial void LogCouldNotRedeem(string type, string name, decimal quantity, string asset);

    [LoggerMessage(12, LogLevel.Information, "{Type} {Name} adjusted quantity of {Quantity} {Asset} up to up {AdjustedQuantity} {Asset} to comply with min lot size of {MinLotSize} {Asset}")]
    private partial void LogAdjustedQuantityToMinLotSize(string type, string name, decimal quantity, string asset, decimal adjustedQuantity, decimal minLotSize);

    [LoggerMessage(13, LogLevel.Information, "{Type} {Name} adjusted quantity of {Quantity} {Asset} at {Price} {Quote} for a total of {Total} {Quote} up to {AdjustedQuantity} {Asset} to match min notional of {MinNotional} {Quote}")]
    private partial void LogAdjustedQuantityToMinNotional(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal adjustedQuantity, decimal minNotional);

    [LoggerMessage(14, LogLevel.Error, "{Type} {Name} will not execute as there are open market orders being executed: {Orders}")]
    private partial void LogConcurrentMarketOrders(string type, string name, IEnumerable<OrderQueryResult> orders);
}