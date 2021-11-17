﻿using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.MarketBuy;

internal partial class MarketBuyCommandExecutor : IAlgoCommandExecutor<MarketBuyCommand>
{
    private readonly ILogger _logger;
    private readonly ISavingsProvider _savings;
    private readonly ISwapPoolProvider _swaps;
    private readonly IBalanceProvider _balances;
    private readonly ITagGenerator _tags;

    public MarketBuyCommandExecutor(ILogger<MarketBuyCommandExecutor> logger, ISavingsProvider savings, ISwapPoolProvider swaps, IBalanceProvider balances, ITagGenerator tags)
    {
        _logger = logger;
        _savings = savings;
        _swaps = swaps;
        _balances = balances;
        _tags = tags;
    }

    private const string TypeName = nameof(MarketBuyCommandExecutor);
    private const OrderType MyOrderType = OrderType.Market;
    private const OrderSide MyOrderSide = OrderSide.Buy;

    public async ValueTask ExecuteAsync(IAlgoContext context, MarketBuyCommand command, CancellationToken cancellationToken = default)
    {
        // get context data for the command symbol
        var ticker = context.Tickers[command.Symbol.Name];
        var spots = context.SpotBalances[command.Symbol.Name];
        var savings = context.SavingsBalances[command.Symbol.Name];
        var swaps = context.SwapPoolBalances[command.Symbol.Name];

        // adjust the quantity up to the min notional to make a valid order
        var quantity = command.Quantity;
        var total = quantity * ticker.ClosePrice;
        total = total.AdjustTotalUpToMinNotional(command.Symbol);
        var adjusted = total / ticker.ClosePrice;
        LogAdjustedQuantityToMinNotional(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, ticker.ClosePrice, command.Symbol.QuoteAsset, total, adjusted, command.Symbol.Filters.MinNotional.MinNotional);

        // adjust the quantity up to the min lot size quantity
        adjusted = quantity.AdjustQuantityUpToMinLotSizeQuantity(command.Symbol);
        LogAdjustedQuantityToMinLotSize(TypeName, command.Symbol.BaseAsset, quantity, command.Symbol.BaseAsset, adjusted, command.Symbol.Filters.LotSize.MinQuantity);
        quantity = adjusted;

        // adjust the quantity up by the step size to make a valid order
        adjusted = quantity.AdjustQuantityUpToLotStepSize(command.Symbol);
        LogAdjustedQuantity(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, adjusted, command.Symbol.Filters.LotSize.StepSize);
        quantity = adjusted;

        // identify the free quote balance

        var free = spots.QuoteAsset.Free
            + (command.RedeemSavings ? savings.QuoteAsset.FreeAmount : 0m)
            + (command.RedeemSwapPool ? swaps.QuoteAsset.Total : 0m);

        // calculate the adjusted total
        total = quantity * ticker.ClosePrice;

        // see if there is enough free balance overall
        if (free < total)
        {
            LogNotEnoughFreeBalance(TypeName, command.Symbol.Name, MyOrderType, MyOrderSide, total, command.Symbol.QuoteAsset, free);
            return;
        }

        // see if we need to redeem anything
        if (total > spots.QuoteAsset.Free)
        {
            // we need to redeem up to this from any redemption sources
            var required = total - spots.QuoteAsset.Free;

            // see if we can redeem the rest from savings
            if (command.RedeemSavings && savings.QuoteAsset.FreeAmount > 0)
            {
                var redeeming = Math.Min(savings.QuoteAsset.FreeAmount, required);

                LogRedeemingSavings(TypeName, command.Symbol.Name, redeeming, command.Symbol.QuoteAsset);

                var result = await new RedeemSavingsCommand(command.Symbol.QuoteAsset, redeeming)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    required -= result.Redeemed;
                    required = Math.Max(required, 0);

                    LogRedeemedSavings(TypeName, command.Symbol.Name, result.Redeemed, command.Symbol.QuoteAsset);
                }
                else
                {
                    LogFailedToRedeemSavings(TypeName, command.Symbol.Name, redeeming, command.Symbol.QuoteAsset);
                    return;
                }
            }

            // see if we can redeem the rest from the swap pool
            if (command.RedeemSwapPool && swaps.QuoteAsset.Total > 0 && required > 0)
            {
                var redeeming = Math.Min(swaps.QuoteAsset.Total, required);

                LogRedeemingSwapPool(TypeName, command.Symbol.Name, redeeming, command.Symbol.QuoteAsset);

                var result = await new RedeemSwapPoolCommand(command.Symbol.QuoteAsset, required)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    required -= result.QuoteAmount;
                    required = Math.Max(required, 0);

                    LogRedeemedSwapPool(TypeName, command.Symbol.Name, result.QuoteAmount, command.Symbol.QuoteAsset);
                }
                else
                {
                    LogFailedToRedeemSwapPool(TypeName, command.Symbol.Name, redeeming, command.Symbol.QuoteAsset);
                    return;
                }
            }

            if (required > 0)
            {
                LogCouldNotRedeem(TypeName, command.Symbol.Name, required, command.Symbol.QuoteAsset);
                return;
            }
        }

        // all set
        var tag = _tags.Generate(command.Symbol.Name, 0);
        await new CreateOrderCommand(command.Symbol, MyOrderType, MyOrderSide, null, quantity, null, tag)
            .ExecuteAsync(context, cancellationToken)
            .ConfigureAwait(false);
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
}