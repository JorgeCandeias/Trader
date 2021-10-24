using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations
{
    internal class EnsureSingleOrderOperation : IEnsureSingleOrderOperation
    {
        private readonly IOptionsMonitor<SavingsOptions> _monitor;
        private readonly ILogger _logger;
        private readonly IBalanceProvider _balances;
        private readonly ICreateOrderOperation _createOrderBlock;
        private readonly IGetOpenOrdersOperation _getOpenOrdersBlock;
        private readonly ICancelOrderOperation _cancelOrderBlock;
        private readonly IRedeemSavingsOperation _redeemSavingsBlock;

        public EnsureSingleOrderOperation(IOptionsMonitor<SavingsOptions> monitor, ILogger<EnsureSingleOrderOperation> logger, IBalanceProvider balances, ICreateOrderOperation orderCreator, IGetOpenOrdersOperation getOpenOrdersBlock, ICancelOrderOperation cancelOrderBlock, IRedeemSavingsOperation redeemSavingsBlock)
        {
            _monitor = monitor;
            _logger = logger;
            _balances = balances;
            _createOrderBlock = orderCreator;
            _getOpenOrdersBlock = getOpenOrdersBlock;
            _cancelOrderBlock = cancelOrderBlock;
            _redeemSavingsBlock = redeemSavingsBlock;
        }

        private static string TypeName => nameof(EnsureSingleOrderOperation);

        public Task<bool> EnsureSingleOrderAsync(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return EnsureSingleOrderCoreAsync(symbol, side, type, timeInForce, quantity, price, redeemSavings, cancellationToken);
        }

        private async Task<bool> EnsureSingleOrderCoreAsync(Symbol symbol, OrderSide side, OrderType type, TimeInForce timeInForce, decimal quantity, decimal price, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            // get current open orders
            var orders = await _getOpenOrdersBlock
                .GetOpenOrdersAsync(symbol, side, cancellationToken)
                .ConfigureAwait(false);

            // cancel all non-desired orders
            var count = orders.Count;
            foreach (var order in orders)
            {
                if (order.Type != type || order.OriginalQuantity != quantity || order.Price != price)
                {
                    await _cancelOrderBlock
                        .CancelOrderAsync(symbol, order.OrderId, cancellationToken)
                        .ConfigureAwait(false);

                    count--;
                }
            }

            // if any order survived then we can stop here
            if (count > 0) return true;

            // get the balance for the affected asset
            var sourceAsset = side switch
            {
                OrderSide.Buy => symbol.QuoteAsset,
                OrderSide.Sell => symbol.BaseAsset,
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };

            var balance = await _balances.GetRequiredBalanceAsync(sourceAsset, cancellationToken).ConfigureAwait(false);

            // get the quantity for the affected asset
            var sourceQuantity = side switch
            {
                OrderSide.Buy => quantity * price,
                OrderSide.Sell => quantity,
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };

            // if there is not enough units to place the order then attempt to redeem from savings
            if (balance.Free < sourceQuantity)
            {
                if (redeemSavings)
                {
                    var necessary = sourceQuantity - balance.Free;

                    var (success, redeemed) = await _redeemSavingsBlock
                        .TryRedeemSavingsAsync(sourceAsset, necessary, cancellationToken)
                        .ConfigureAwait(false);

                    if (success)
                    {
                        var delay = _monitor.CurrentValue.SavingsRedemptionDelay;

                        _logger.LogInformation(
                            "{Type} {Name} redeemed {Redeemed:F8} {Asset} from savings to cover the necessary {Necessary:F8} {Asset} and will wait {Wait} for the operation to complete",
                            TypeName, symbol.Name, redeemed, sourceAsset, necessary, sourceAsset, delay);

                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "{type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from savings",
                            TypeName, symbol.Name, necessary, sourceAsset);

                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity:F8} {Asset} for {Price:F8} {Quote} but there is only {Free:F8} {Asset} available and savings redemption is disabled",
                        TypeName, symbol.Name, type, side, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, balance.Free, sourceAsset);

                    return false;
                }
            }

            // if we got here then we can place the order
            await _createOrderBlock
                .CreateOrderAsync(symbol, type, side, timeInForce, quantity, price, $"{symbol.Name}{price:F8}".Replace(".", "", StringComparison.Ordinal), cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
    }
}