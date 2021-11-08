using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder
{
    internal class EnsureSingleOrderExecutor : IAlgoCommandExecutor<EnsureSingleOrderCommand>
    {
        private readonly ILogger _logger;
        private readonly IBalanceProvider _balances;
        private readonly IOrderProvider _orders;

        public EnsureSingleOrderExecutor(ILogger<EnsureSingleOrderExecutor> logger, IBalanceProvider balances, IOrderProvider orders)
        {
            _logger = logger;
            _balances = balances;
            _orders = orders;
        }

        private static string TypeName => nameof(EnsureSingleOrderExecutor);

        public async Task ExecuteAsync(IAlgoContext context, EnsureSingleOrderCommand command, CancellationToken cancellationToken = default)
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
                        _logger.LogInformation(
                            "{Type} {Name} redeemed {Redeemed:F8} {Asset} from savings to cover the necessary {Necessary:F8} {Asset} and will let the calling algo cycle",
                            TypeName, command.Symbol.Name, result.Redeemed, sourceAsset, necessary, sourceAsset);

                        return;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "{type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from savings",
                            TypeName, command.Symbol.Name, necessary, sourceAsset);

                        var result2 = await new RedeemSwapPoolCommand(sourceAsset, necessary)
                            .ExecuteAsync(context, cancellationToken)
                            .ConfigureAwait(false);

                        if (result2.Success)
                        {
                            _logger.LogInformation(
                                "{Type} {Name} redeemed {Redeemed:F8} {Asset} from the swap pool to cover the necessary {Necessary:F8} {Asset} and will wait let the calling algo cycle",
                                TypeName, command.Symbol.Name, result2.QuoteAmount, sourceAsset, necessary, sourceAsset);

                            return;
                        }
                        else
                        {
                            _logger.LogWarning(
                                "{type} {Name} could not redeem the necessary {Necessary:F8} {Asset} from a swap pool",
                                TypeName, command.Symbol.Name, necessary, sourceAsset);

                            return;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity:F8} {Asset} for {Price:F8} {Quote} but there is only {Free:F8} {Asset} available and savings redemption is disabled",
                        TypeName, command.Symbol.Name, command.Type, command.Side, command.Quantity, command.Symbol.BaseAsset, command.Price, command.Symbol.QuoteAsset, balance.Free, sourceAsset);

                    return;
                }
            }

            // if we got here then we can place the order
            var tag = $"{command.Symbol.Name}{command.Price:F8}".Replace(".", "", StringComparison.Ordinal);
            await new CreateOrderCommand(command.Symbol, command.Type, command.Side, command.TimeInForce, command.Quantity, command.Price, tag)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}