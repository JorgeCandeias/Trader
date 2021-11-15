using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder
{
    internal partial class EnsureSingleOrderExecutor : IAlgoCommandExecutor<EnsureSingleOrderCommand>
    {
        private readonly ILogger _logger;
        private readonly IBalanceProvider _balances;
        private readonly IOrderProvider _orders;
        private readonly ITagGenerator _tags;

        public EnsureSingleOrderExecutor(ILogger<EnsureSingleOrderExecutor> logger, IBalanceProvider balances, IOrderProvider orders, ITagGenerator tags)
        {
            _logger = logger;
            _balances = balances;
            _orders = orders;
            _tags = tags;
        }

        private const string TypeName = nameof(EnsureSingleOrderExecutor);

        public async ValueTask ExecuteAsync(IAlgoContext context, EnsureSingleOrderCommand command, CancellationToken cancellationToken = default)
        {
            // get current open orders
            var orders = await _orders
                .GetOrdersByFilterAsync(command.Symbol.Name, command.Side, true, null, cancellationToken)
                .ConfigureAwait(false);

            // cancel all non-desired orders
            var count = orders.Count;
            foreach (var order in orders)
            {
                if (order.Type != command.Type || order.OriginalQuantity != command.Quantity || order.Price != command.Price)
                {
                    await new CancelOrderCommand(command.Symbol, order.OrderId)
                        .ExecuteAsync(context, cancellationToken)
                        .ConfigureAwait(false);

                    count--;
                }
            }

            // if any order survived then we can stop here
            if (count > 0) return;

            // get the balance for the affected asset
            var sourceAsset = command.Side switch
            {
                OrderSide.Buy => command.Symbol.QuoteAsset,
                OrderSide.Sell => command.Symbol.BaseAsset,
                _ => throw new InvalidOperationException()
            };

            var balance = await _balances.GetRequiredBalanceAsync(sourceAsset, cancellationToken).ConfigureAwait(false);

            // get the quantity for the affected asset
            var sourceQuantity = command.Side switch
            {
                OrderSide.Buy => command.Quantity * command.Price,
                OrderSide.Sell => command.Quantity,
                _ => throw new InvalidOperationException()
            };

            // if there is not enough units to place the order then attempt to redeem from savings
            if (balance.Free < sourceQuantity)
            {
                if (command.RedeemSavings)
                {
                    var necessary = sourceQuantity - balance.Free;

                    var result = await new RedeemSavingsCommand(sourceAsset, necessary)
                        .ExecuteAsync(context, cancellationToken)
                        .ConfigureAwait(false);

                    if (result.Success)
                    {
                        LogRedeemedFromSavings(TypeName, command.Symbol.Name, result.Redeemed, sourceAsset, necessary);

                        return;
                    }
                    else
                    {
                        LogCouldNotRedeemFromSavings(TypeName, command.Symbol.Name, necessary, sourceAsset);

                        var result2 = await new RedeemSwapPoolCommand(sourceAsset, necessary)
                            .ExecuteAsync(context, cancellationToken)
                            .ConfigureAwait(false);

                        if (result2.Success)
                        {
                            LogRedeemedFromSwapPool(TypeName, command.Symbol.Name, result2.QuoteAmount, sourceAsset, necessary);

                            return;
                        }
                        else
                        {
                            LogCouldNotRedeemFromSwapPool(TypeName, command.Symbol.Name, necessary, sourceAsset);

                            return;
                        }
                    }
                }
                else
                {
                    LogMustPlaceOrderButRedemptionIsDisabled(TypeName, command.Symbol.Name, command.Type, command.Side, command.Quantity, command.Symbol.BaseAsset, command.Price, command.Symbol.QuoteAsset, balance.Free, sourceAsset);

                    return;
                }
            }

            // if we got here then we can place the order
            var tag = _tags.Generate(command.Symbol.Name, command.Price);
            await new CreateOrderCommand(command.Symbol, command.Type, command.Side, command.TimeInForce, command.Quantity, command.Price, tag)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} redeemed {Redeemed:F8} {Asset} from savings to cover the necessary {Necessary:F8} {Asset} and will let the calling algo cycle")]
        private partial void LogRedeemedFromSavings(string type, string name, decimal redeemed, string asset, decimal necessary);

        [LoggerMessage(0, LogLevel.Warning, "{Type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from savings")]
        private partial void LogCouldNotRedeemFromSavings(string type, string name, decimal necessary, string asset);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} redeemed {Redeemed:F8} {Asset} from the swap pool to cover the necessary {Necessary:F8} {Asset} and will wait let the calling algo cycle")]
        private partial void LogRedeemedFromSwapPool(string type, string name, decimal redeemed, string asset, decimal necessary);

        [LoggerMessage(0, LogLevel.Warning, "{Type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from a swap pool")]
        private partial void LogCouldNotRedeemFromSwapPool(string type, string name, decimal necessary, string asset);

        [LoggerMessage(0, LogLevel.Warning, "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity:F8} {Asset} for {Price:F8} {Quote} but there is only {Free:F8} {SourceAsset} available and savings redemption is disabled")]
        private partial void LogMustPlaceOrderButRedemptionIsDisabled(string type, string name, OrderType orderType, OrderSide orderSide, decimal quantity, string asset, decimal price, string quote, decimal free, string sourceAsset);

        #endregion Logging
    }
}