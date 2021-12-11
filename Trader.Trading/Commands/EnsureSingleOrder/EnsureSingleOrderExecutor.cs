using Microsoft.Extensions.Logging;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder;

internal partial class EnsureSingleOrderExecutor : IAlgoCommandExecutor<EnsureSingleOrderCommand>
{
    private readonly ILogger _logger;

    public EnsureSingleOrderExecutor(ILogger<EnsureSingleOrderExecutor> logger)
    {
        _logger = logger;
    }

    public async ValueTask ExecuteAsync(IAlgoContext context, EnsureSingleOrderCommand command, CancellationToken cancellationToken = default)
    {
        // get context data
        var data = context.Data[command.Symbol.Name];
        var orders = data.Orders.Open.Where(x => x.Side == command.Side);

        // cancel all non-desired orders
        var live = 0;
        foreach (var order in orders)
        {
            if (order.Type == command.Type &&
                (command.Quantity is null || order.OriginalQuantity == command.Quantity.Value) &&
                (command.Notional is null || order.OriginalQuoteOrderQuantity == command.Notional.Value) &&
                (command.Price is null || order.Price == command.Price.Value) &&
                (command.StopPrice is null || order.StopPrice == command.StopPrice.Value) &&
                (command.Tag is null || (command.Tag == order.ClientOrderId)))
            {
                live++;
            }
            else
            {
                await new CancelOrderCommand(command.Symbol, order.OrderId)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // stop here if orders survived - this means the target order is already set
        if (live > 0)
        {
            return;
        }

        // if we got here then we can place the order
        await new CreateOrderCommand(command.Symbol, command.Type, command.Side, command.TimeInForce, command.Quantity, command.Notional, command.Price, command.StopPrice, command.Tag)
            .ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} {Name} redeemed {Redeemed:F8} {Asset} from savings to cover the necessary {Necessary:F8} {Asset} and will let the calling algo cycle")]
    private partial void LogRedeemedFromSavings(string type, string name, decimal redeemed, string asset, decimal necessary);

    [LoggerMessage(1, LogLevel.Warning, "{Type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from savings")]
    private partial void LogCouldNotRedeemFromSavings(string type, string name, decimal necessary, string asset);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} redeemed {Redeemed:F8} {Asset} from the swap pool to cover the necessary {Necessary:F8} {Asset} and will wait let the calling algo cycle")]
    private partial void LogRedeemedFromSwapPool(string type, string name, decimal redeemed, string asset, decimal necessary);

    [LoggerMessage(3, LogLevel.Warning, "{Type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from a swap pool")]
    private partial void LogCouldNotRedeemFromSwapPool(string type, string name, decimal necessary, string asset);

    [LoggerMessage(4, LogLevel.Warning, "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity:F8} {Asset} at {Price:F8} {Quote} for a total of {Notional:F8} {Quote} but there is only {Free:F8} {SourceAsset} available and savings redemption is disabled")]
    private partial void LogMustPlaceOrderButRedemptionIsDisabled(string type, string name, OrderType orderType, OrderSide orderSide, decimal? quantity, string asset, decimal? price, string quote, decimal? notional, decimal free, string sourceAsset);

    #endregion Logging
}