using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.MarketBuy
{
    internal partial class MarketBuyCommandExecutor : IAlgoCommandExecutor<MarketBuyCommand>
    {
        private readonly ILogger _logger;
        private readonly ISavingsProvider _savings;
        private readonly ISwapPoolProvider _swaps;
        private readonly IBalanceProvider _balances;

        public MarketBuyCommandExecutor(ILogger<MarketBuyCommandExecutor> logger, ISavingsProvider savings, ISwapPoolProvider swaps, IBalanceProvider balances)
        {
            _logger = logger;
            _savings = savings;
            _swaps = swaps;
            _balances = balances;
        }

        private const string TypeName = nameof(MarketBuyCommandExecutor);
        private const OrderType MyOrderType = OrderType.Market;
        private const OrderSide MyOrderSide = OrderSide.Buy;

        public async ValueTask ExecuteAsync(IAlgoContext context, MarketBuyCommand command, CancellationToken cancellationToken = default)
        {
            // adjust the quantity down by the step size to make a valid order
            var quantity = command.Quantity.AdjustQuantityDownToLotStepSize(context.Symbol);
            LogAdjustedQuantity(TypeName, context.Symbol.Name, command.Quantity, context.Symbol.BaseAsset, quantity, context.Symbol.Filters.LotSize.StepSize);

            // if the quantity becomes lower than the minimum lot size then we cant buy
            if (quantity < context.Symbol.Filters.LotSize.MinQuantity)
            {
                LogQuantityLessThanMinLotSize(TypeName, context.Symbol.Name, MyOrderType, MyOrderSide, quantity, context.Symbol.BaseAsset, context.Symbol.Filters.LotSize.MinQuantity);
                return;
            }

            // if the total becomes lower than the minimum notional then we cant buy
            var total = quantity * context.Ticker.ClosePrice;
            if (total < context.Symbol.Filters.MinNotional.MinNotional)
            {
                LogTotalLessThanMinNotional(TypeName, context.Symbol.Name, MyOrderType, MyOrderSide, quantity, context.Symbol.BaseAsset, context.Ticker.ClosePrice, context.Symbol.QuoteAsset, total, context.Symbol.Filters.MinNotional.MinNotional);
                return;
            }

            // identify the free quote balance
            var free = context.QuoteAssetSpotBalance.Free
                + (command.RedeemSavings ? context.QuoteAssetSavingsBalance.FreeAmount : 0m)
                + (command.RedeemSwapPool ? context.QuoteAssetSwapPoolBalance.Total : 0m);

            // see if there is enough free balance overall
            if (free < total)
            {
                LogNotEnoughFreeBalance(TypeName, context.Symbol.Name, MyOrderType, MyOrderSide, total, context.Symbol.QuoteAsset, free);
                return;
            }

            // see if we need to redeem anything
            if (total > context.QuoteAssetSpotBalance.Free)
            {
                // we need to redeem up to this from any redemption sources
                var required = total - context.QuoteAssetSpotBalance.Free;

                // see if we can redeem the rest from savings
                if (command.RedeemSavings && context.QuoteAssetSavingsBalance.FreeAmount > 0)
                {
                    var redeeming = Math.Min(context.QuoteAssetSavingsBalance.FreeAmount, required);

                    LogRedeemingSavings(TypeName, context.Symbol.Name, redeeming, context.Symbol.QuoteAsset);

                    var result = await new RedeemSavingsCommand(context.Symbol.QuoteAsset, redeeming)
                        .ExecuteAsync(context, cancellationToken)
                        .ConfigureAwait(false);

                    if (result.Success)
                    {
                        required -= result.Redeemed;
                        required = Math.Max(required, 0);
                    }
                }

                // see if we can redeem the rest from the swap pool
                if (command.RedeemSwapPool && context.QuoteAssetSwapPoolBalance.Total > 0 && required > 0)
                {
                    var redeeming = Math.Min(context.QuoteAssetSwapPoolBalance.Total, required);

                    LogRedeemingSwapPool(TypeName, context.Symbol.Name, redeeming, context.Symbol.QuoteAsset);

                    var result = await new RedeemSwapPoolCommand(context.Symbol.QuoteAsset, required)
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
                    LogCouldNotRedeem(TypeName, context.Symbol.Name, required, context.Symbol.QuoteAsset);
                    return;
                }
            }

            // all set
            await new CreateOrderCommand(context.Symbol, MyOrderType, MyOrderSide, null, quantity, null, null)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} adjusted original quantity of {Quantity} {Asset} down to {AdjustedQuantity} {Asset} by step size {StepSize} {Asset}")]
        private partial void LogAdjustedQuantity(string type, string name, decimal quantity, string asset, decimal adjustedQuantity, decimal stepSize);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} because it is less than the minimum lot size of {MinLotSize} {Asset}")]
        private partial void LogQuantityLessThanMinLotSize(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal minLotSize);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} and price {Price} {Quote} because the total of {Total} {Quote} is less than the minimum notional of {MinNotional} {Quote}")]
        private partial void LogTotalLessThanMinNotional(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot place {OrderType} {OrderSide} order with quantity {Quantity} {Asset} because the free amount from all sources is only {Free} {Asset}")]
        private partial void LogNotEnoughFreeBalance(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal free);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from savings")]
        private partial void LogRedeemingSavings(string type, string name, decimal quantity, string asset);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from the swap pool")]
        private partial void LogRedeemingSwapPool(string type, string name, decimal quantity, string asset);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} could not redeem the required {Quantity} {Asset}")]
        private partial void LogCouldNotRedeem(string type, string name, decimal quantity, string asset);
    }
}