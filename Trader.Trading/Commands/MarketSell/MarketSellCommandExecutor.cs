using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;

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

    public async ValueTask ExecuteAsync(IAlgoContext context, MarketSellCommand command, CancellationToken cancellationToken = default)
    {
        // get context data for the command symbol
        var data = context.Data[command.Symbol.Name];
        var ticker = data.Ticker;
        var spots = data.Spot;
        var savings = context.SavingsBalances[command.Symbol.Name];
        var swaps = context.SwapPoolBalances[command.Symbol.Name];

        // adjust the quantity down by the step size to make a valid order
        var quantity = command.Quantity.AdjustQuantityDownToLotStepSize(command.Symbol);
        LogAdjustedQuantity(TypeName, command.Symbol.Name, command.Quantity, command.Symbol.BaseAsset, quantity, command.Symbol.Filters.LotSize.StepSize);

        // if the quantity becomes lower than the minimum lot size then we cant sell
        if (quantity < command.Symbol.Filters.LotSize.MinQuantity)
        {
            LogQuantityLessThanMinLotSize(TypeName, command.Symbol.Name, MyOrderType, MyOrderSide, quantity, command.Symbol.BaseAsset, command.Symbol.Filters.LotSize.MinQuantity);
            return;
        }

        // if the total becomes lower than the minimum notional then we cant sell
        var total = quantity * ticker.ClosePrice;
        if (total < command.Symbol.Filters.MinNotional.MinNotional)
        {
            LogTotalLessThanMinNotional(TypeName, command.Symbol.Name, MyOrderType, MyOrderSide, quantity, command.Symbol.BaseAsset, ticker.ClosePrice, command.Symbol.QuoteAsset, total, command.Symbol.Filters.MinNotional.MinNotional);
            return;
        }

        // identify the free balance
        var free = spots.BaseAsset.Free
            + (command.RedeemSavings ? savings.BaseAsset.FreeAmount : 0m)
            + (command.RedeemSwapPool ? swaps.BaseAsset.Total : 0m);

        // see if there is enough free balance overall
        if (free < quantity)
        {
            LogNotEnoughFreeBalance(TypeName, command.Symbol.Name, MyOrderType, MyOrderSide, quantity, command.Symbol.BaseAsset, free);
            return;
        }

        // see if we need to redeem anything
        if (quantity > spots.BaseAsset.Free)
        {
            // we need to redeem up to this from any redemption sources
            var required = quantity - spots.BaseAsset.Free;

            // see if we can redeem the rest from savings
            if (command.RedeemSavings && savings.BaseAsset.FreeAmount > 0)
            {
                var redeeming = Math.Min(savings.BaseAsset.FreeAmount, required);

                LogRedeemingSavings(TypeName, command.Symbol.Name, redeeming, command.Symbol.BaseAsset);

                var result = await new RedeemSavingsCommand(command.Symbol.BaseAsset, redeeming)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    required -= result.Redeemed;
                    required = Math.Max(required, 0);
                }
            }

            // see if we can redeem the rest from the swap pool
            if (command.RedeemSwapPool && swaps.BaseAsset.Total > 0 && required > 0)
            {
                var redeeming = Math.Min(swaps.BaseAsset.Total, required);

                LogRedeemingSwapPool(TypeName, command.Symbol.Name, redeeming, command.Symbol.BaseAsset);

                var result = await new RedeemSwapPoolCommand(command.Symbol.BaseAsset, required)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    required -= result.QuoteAmount;
                    required = Math.Max(required, 0);
                }
            }

            if (required > 0)
            {
                LogCouldNotRedeem(TypeName, command.Symbol.Name, required, command.Symbol.BaseAsset);
                return;
            }
        }

        // all set
        await new CreateOrderCommand(command.Symbol, OrderType.Market, OrderSide.Sell, null, quantity, null, null)
            .ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);
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
}