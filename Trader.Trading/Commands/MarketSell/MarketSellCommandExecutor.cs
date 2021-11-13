using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.MarketSell
{
    internal partial class MarketSellCommandExecutor : IAlgoCommandExecutor<MarketSellCommand>
    {
        private readonly ILogger _logger;
        private readonly ISavingsProvider _savings;
        private readonly ISwapPoolProvider _swaps;
        private readonly IBalanceProvider _balances;

        public MarketSellCommandExecutor(ILogger<MarketSellCommandExecutor> logger, ISavingsProvider savings, ISwapPoolProvider swaps, IBalanceProvider balances)
        {
            _logger = logger;
            _savings = savings;
            _swaps = swaps;
            _balances = balances;
        }

        private const string TypeName = nameof(MarketSellCommandExecutor);

        public async ValueTask ExecuteAsync(IAlgoContext context, MarketSellCommand command, CancellationToken cancellationToken = default)
        {
            // adjust the quantity down by the step size to make a valid order
            var quantity = command.Quantity.AdjustQuantityDownToLotStepSize(context.Symbol);
            LogAdjustedQuantity(TypeName, context.Symbol.Name, command.Quantity, context.Symbol.BaseAsset, quantity, context.Symbol.Filters.LotSize.StepSize);

            // if the quantity becomes lower than the minimum lot size then we cant sell
            if (quantity < context.Symbol.Filters.LotSize.MinQuantity)
            {
                LogQuantityLessThanMinLotSize(TypeName, context.Symbol.Name, quantity, context.Symbol.BaseAsset, context.Symbol.Filters.LotSize.MinQuantity);
                return;
            }

            // if the total becomes lower than the minimum notional then we cant sell
            var total = quantity * context.Ticker.ClosePrice;
            if (total < context.Symbol.Filters.MinNotional.MinNotional)
            {
                LogTotalLessThanMinNotional(TypeName, context.Symbol.Name, quantity, context.Symbol.BaseAsset, context.Ticker.ClosePrice, context.Symbol.QuoteAsset, total, context.Symbol.Filters.MinNotional.MinNotional);
                return;
            }

            // get present balances
            var balance = await _balances.GetBalanceOrZeroAsync(context.Symbol.BaseAsset, cancellationToken);
            var savings = await _savings.GetPositionOrZeroAsync(context.Symbol.BaseAsset, cancellationToken);
            var pool = await _swaps.GetBalanceAsync(context.Symbol.BaseAsset, cancellationToken);

            // identify the free balance
            var free = balance.Free
                + (command.RedeemSavings ? savings.FreeAmount : 0m)
                + (command.RedeemSwapPool ? pool.Total : 0m);

            // see if there is enough free balance overall
            if (free < quantity)
            {
                LogNotEnoughFreeBalance(TypeName, context.Symbol.Name, quantity, context.Symbol.BaseAsset, free);
                return;
            }

            // see if we need to redeem anything
            if (quantity > balance.Free)
            {
                // we need to redeem up to this from any redemption sources
                var required = quantity - balance.Free;

                // see if we can redeem the rest from savings
                if (command.RedeemSavings && savings.FreeAmount > 0)
                {
                    var redeeming = Math.Min(savings.FreeAmount, required);

                    LogRedeemingSavings(TypeName, context.Symbol.Name, redeeming, context.Symbol.BaseAsset);

                    var result = await new RedeemSavingsCommand(context.Symbol.BaseAsset, redeeming)
                        .ExecuteAsync(context, cancellationToken)
                        .ConfigureAwait(false);

                    if (result.Success)
                    {
                        required -= result.Redeemed;
                        required = Math.Max(required, 0);
                    }
                }

                // see if we can redeem the rest from the swap pool
                if (command.RedeemSwapPool && pool.Total > 0 && required > 0)
                {
                    var redeeming = Math.Min(pool.Total, required);

                    LogRedeemingSwapPool(TypeName, context.Symbol.Name, redeeming, context.Symbol.BaseAsset);

                    var result = await new RedeemSwapPoolCommand(context.Symbol.BaseAsset, required)
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
                    LogCouldNotRedeem(TypeName, context.Symbol.Name, required, context.Symbol.BaseAsset);
                    return;
                }
            }

            // all set
            await new CreateOrderCommand(context.Symbol, OrderType.Market, OrderSide.Sell, null, quantity, null, null)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} adjusted original quantity of {Quantity} {Asset} down to {AdjustedQuantity} {Asset} by step size {StepSize} {Asset}")]
        private partial void LogAdjustedQuantity(string type, string name, decimal quantity, string asset, decimal adjustedQuantity, decimal stepSize);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot place order with quantity {Quantity} {Asset} because it is less than the minimum lot size of {MinLotSize} {Asset}")]
        private partial void LogQuantityLessThanMinLotSize(string type, string name, decimal quantity, string asset, decimal minLotSize);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot place order with quantity {Quantity} {Asset} and price {Price} {Quote} because the total of {Total} {Quote} is less than the minimum notional of {MinNotional} {Quote}")]
        private partial void LogTotalLessThanMinNotional(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot place order with quantity {Quantity} {Asset} because the free amount from all sources is only {Free} {Asset}")]
        private partial void LogNotEnoughFreeBalance(string type, string name, decimal quantity, string asset, decimal free);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from savings")]
        private partial void LogRedeemingSavings(string type, string name, decimal quantity, string asset);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} attempting to redeem {Quantity} {Asset} from the swap pool")]
        private partial void LogRedeemingSwapPool(string type, string name, decimal quantity, string asset);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} could not redeem the required {Quantity} {Asset}")]
        private partial void LogCouldNotRedeem(string type, string name, decimal quantity, string asset);
    }
}