using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.SignificantAveragingSell
{
    internal class SignificantAveragingSellExecutor : IAlgoCommandExecutor<SignificantAveragingSellCommand>
    {
        private readonly ILogger _logger;

        public SignificantAveragingSellExecutor(ILogger<SignificantAveragingSellExecutor> logger)
        {
            _logger = logger;
        }

        private static string TypeName => nameof(SignificantAveragingSellExecutor);

        public Task ExecuteAsync(IAlgoContext context, SignificantAveragingSellCommand command, CancellationToken cancellationToken = default)
        {
            // calculate the desired sell
            var desired = CalculateDesiredSell(command.Symbol, command.MinimumProfitRate, command.Orders, command.Ticker);

            // apply the desired sell
            if (desired == DesiredSell.None)
            {
                return new ClearOpenOrdersCommand(command.Symbol, OrderSide.Sell)
                    .ExecuteAsync(context, cancellationToken);
            }
            else
            {
                return new EnsureSingleOrderCommand(command.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, desired.Quantity, desired.Price, command.RedeemSavings)
                    .ExecuteAsync(context, cancellationToken);
            }
        }

        private DesiredSell CalculateDesiredSell(Symbol symbol, decimal minimumProfitRate, IReadOnlyCollection<OrderQueryResult> orders, MiniTicker ticker)
        {
            // skip if there is nothing to sell
            if (orders.Count == 0)
            {
                return DesiredSell.None;
            }

            // elect lowest significant orders that fit under the minimum profit rate when sold
            var count = 0;
            var numerator = 0m;
            var quantity = 0m;

            foreach (var order in orders.OrderBy(x => x.Price))
            {
                // calculate the candidate average sell price
                var orderNumerator = order.ExecutedQuantity * order.Price;
                var orderQuantity = order.ExecutedQuantity;
                var candidateNumerator = numerator + orderNumerator;
                var candidateQuantity = quantity + orderQuantity;
                var candidateAverageBuyPrice = candidateNumerator / candidateQuantity;
                var candidateSellPrice = candidateAverageBuyPrice * minimumProfitRate;

                // adjust the candidate average sell price up to the tick size
                candidateSellPrice = candidateSellPrice.AdjustPriceUpToTickSize(symbol);

                // elect the order if the candidate average sell price is below the ticker
                if (candidateSellPrice <= ticker.ClosePrice)
                {
                    count++;
                    numerator = candidateNumerator;
                    quantity = candidateQuantity;
                }
                else
                {
                    break;
                }
            }

            // skip if no buy orders were elected for selling
            if (count <= 0)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} cannot elect any buy orders for selling at a minimum profit rate of {MinimumProfitRate}",
                    TypeName, symbol.Name, minimumProfitRate);

                return DesiredSell.None;
            }

            // adjust the quantity down to the lot size filter
            quantity = quantity.AdjustQuantityDownToLotStepSize(symbol);

            // break if the quantity is under the minimum lot size
            if (quantity < symbol.Filters.LotSize.MinQuantity)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, symbol.Filters.LotSize.MinQuantity, symbol.BaseAsset);

                return DesiredSell.None;
            }

            // calculate the sell notional
            var total = quantity * ticker.ClosePrice;

            // check if the sell is under the minimum notional filter
            if (total < symbol.Filters.MinNotional.MinNotional)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, ticker.ClosePrice, symbol.QuoteAsset, quantity * ticker.ClosePrice, symbol.QuoteAsset, symbol.Filters.MinNotional.MinNotional, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // otherwise we now have a valid desired sell
            return new DesiredSell(quantity, ticker.ClosePrice);
        }

        private sealed record DesiredSell(decimal Quantity, decimal Price)
        {
            public static readonly DesiredSell None = new(0m, 0m);
        }
    }
}