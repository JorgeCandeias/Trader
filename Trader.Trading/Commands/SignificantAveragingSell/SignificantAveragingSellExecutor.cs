using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.SignificantAveragingSell
{
    internal partial class SignificantAveragingSellExecutor : IAlgoCommandExecutor<SignificantAveragingSellCommand>
    {
        private readonly ILogger _logger;

        public SignificantAveragingSellExecutor(ILogger<SignificantAveragingSellExecutor> logger)
        {
            _logger = logger;
        }

        private const string TypeName = nameof(SignificantAveragingSellExecutor);

        public ValueTask ExecuteAsync(IAlgoContext context, SignificantAveragingSellCommand command, CancellationToken cancellationToken = default)
        {
            // calculate the desired sell
            var desired = CalculateDesiredSell(command);

            // apply the desired sell
            if (desired == DesiredSell.None)
            {
                return new ClearOpenOrdersCommand(command.Symbol, OrderSide.Sell)
                    .ExecuteAsync(context, cancellationToken);
            }
            else
            {
                return new EnsureSingleOrderCommand(command.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, desired.Quantity, desired.Price, command.RedeemSavings, command.RedeemSwapPool)
                    .ExecuteAsync(context, cancellationToken);
            }
        }

        private DesiredSell CalculateDesiredSell(SignificantAveragingSellCommand command)
        {
            // skip if there is nothing to sell
            if (command.Orders.Count == 0)
            {
                return DesiredSell.None;
            }

            // elect lowest significant orders that fit under the minimum profit rate when sold
            var count = 0;
            var numerator = 0m;
            var quantity = 0m;

            foreach (var order in command.Orders.Reverse())
            {
                // calculate the candidate average sell price
                var orderNumerator = order.ExecutedQuantity * order.Price;
                var orderQuantity = order.ExecutedQuantity;
                var candidateNumerator = numerator + orderNumerator;
                var candidateQuantity = quantity + orderQuantity;
                var candidateAverageBuyPrice = candidateNumerator / candidateQuantity;
                var candidateSellPrice = candidateAverageBuyPrice * command.MinimumProfitRate;

                // adjust the candidate average sell price up to the tick size
                candidateSellPrice = candidateSellPrice.AdjustPriceUpToTickSize(command.Symbol);

                // elect the order if the candidate average sell price is below the ticker
                if (candidateSellPrice <= command.Ticker.ClosePrice)
                {
                    count++;
                    numerator = candidateNumerator;
                    quantity = candidateQuantity;

                    LogElectedOrder(TypeName, command.Symbol.Name, order.OrderId, order.ExecutedQuantity, command.Symbol.BaseAsset, order.Price, command.Symbol.QuoteAsset);
                }
                else
                {
                    break;
                }
            }

            // skip if no buy orders were elected for selling
            if (count <= 0)
            {
                LogCannotElectedBuyOrders(TypeName, command.Symbol.Name, command.MinimumProfitRate);

                return DesiredSell.None;
            }

            // calculate average buy price
            var averagePrice = numerator / quantity;

            LogElectedOrders(TypeName, command.Symbol.Name, count, quantity, command.Symbol.BaseAsset, averagePrice, command.Symbol.QuoteAsset);

            // adjust the quantity down to the lot size filter
            quantity = quantity.AdjustQuantityDownToLotStepSize(command.Symbol);

            LogAdjustedQuantityByLotStepSize(TypeName, command.Symbol.Name, command.Symbol.Filters.LotSize.StepSize, command.Symbol.BaseAsset, quantity);

            // break if the quantity is under the minimum lot size
            if (quantity < command.Symbol.Filters.LotSize.MinQuantity)
            {
                LogCannotSetSellOrder(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, command.Symbol.Filters.LotSize.MinQuantity);

                return DesiredSell.None;
            }

            // calculate the sell notional
            var total = quantity * command.Ticker.ClosePrice;

            LogCalculatedNotional(TypeName, command.Symbol.Name, total, command.Symbol.QuoteAsset, quantity, command.Symbol.BaseAsset, command.Ticker.ClosePrice);

            // check if the sell is under the minimum notional filter
            if (total < command.Symbol.Filters.MinNotional.MinNotional)
            {
                LogCannotSetSellOrderUnderMinimumNotional(TypeName, command.Symbol.Name, quantity, command.Symbol.BaseAsset, command.Ticker.ClosePrice, command.Symbol.QuoteAsset, quantity * command.Ticker.ClosePrice, command.Symbol.Filters.MinNotional.MinNotional);

                return DesiredSell.None;
            }

            // otherwise we now have a valid desired sell
            return new DesiredSell(quantity, command.Ticker.ClosePrice);
        }

        private sealed record DesiredSell(decimal Quantity, decimal Price)
        {
            public static readonly DesiredSell None = new(0m, 0m);
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} elected order {OrderId} for sale with significant quantity {Quantity:F8} {Asset} at buy price {Price:F8} {Quote}")]
        private partial void LogElectedOrder(string type, string name, long orderId, decimal quantity, string asset, decimal price, string quote);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} cannot elect any buy orders for selling at a minimum profit rate of {MinimumProfitRate:F8}")]
        private partial void LogCannotElectedBuyOrders(string type, string name, decimal minimumProfitRate);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} elected {Count} orders for sale with total quantity {Quantity:F8} {Asset} at average buy price {Price:F8} {Quote}")]
        private partial void LogElectedOrders(string type, string name, int count, decimal quantity, string asset, decimal price, string quote);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} adjusted quantity by lot step size of {LotStepSize} {Asset} down to {Quantity:F8} {Asset}")]
        private partial void LogAdjustedQuantityByLotStepSize(string type, string name, decimal lotStepSize, string asset, decimal quantity);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}")]
        private partial void LogCannotSetSellOrder(string type, string name, decimal quantity, string asset, decimal minLotSize);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} calculated notional of {Total:F8} {Quote} using quantity of {Quantity:F8} {Asset} and ticker of {Price:F8} {Quote}")]
        private partial void LogCalculatedNotional(string type, string name, decimal total, string quote, decimal quantity, string asset, decimal price);

        [LoggerMessage(0, LogLevel.Error, "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}")]
        private partial void LogCannotSetSellOrderUnderMinimumNotional(string type, string name, decimal quantity, string asset, decimal price, string quote, decimal total, decimal minNotional);

        #endregion Logging
    }
}