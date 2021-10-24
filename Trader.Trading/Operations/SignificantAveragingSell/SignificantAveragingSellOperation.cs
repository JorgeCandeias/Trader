using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.SignificantAveragingSell
{
    internal class SignificantAveragingSellOperation : ISignificantAveragingSellOperation
    {
        private readonly ILogger _logger;
        private readonly IClearOpenOrdersOperation _clearOpenOrdersBlock;
        private readonly IEnsureSingleOrderOperation _ensureSingleOrderBlock;

        public SignificantAveragingSellOperation(ILogger<SignificantAveragingSellOperation> logger, IClearOpenOrdersOperation clearOpenOrdersBlock, IEnsureSingleOrderOperation ensureSingleOrderBlock)
        {
            _logger = logger;
            _clearOpenOrdersBlock = clearOpenOrdersBlock;
            _ensureSingleOrderBlock = ensureSingleOrderBlock;
        }

        private static string TypeName => nameof(SignificantAveragingSellOperation);

        public Task SetSignificantAveragingSellAsync(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));
            if (ticker is null) throw new ArgumentNullException(nameof(ticker));
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            foreach (var order in orders)
            {
                if (order.Side != OrderSide.Buy)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Parameter '{nameof(orders)}' must only contain orders with side '{OrderSide.Buy}'");
                }
                else if (order.ExecutedQuantity <= 0m)
                {
                    throw new ArgumentOutOfRangeException(nameof(orders), $"Parameter '{nameof(orders)}' must only contain orders with executed quantity greater than zero'");
                }
            }

            return SetSignificantAveragingSellCoreAsync(symbol, ticker, orders, minimumProfitRate, redeemSavings, cancellationToken);
        }

        private async Task SetSignificantAveragingSellCoreAsync(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, CancellationToken cancellationToken)
        {
            // calculate the desired sell
            var desired = CalculateDesiredSell(symbol, minimumProfitRate, orders, ticker);

            // apply the desired sell
            if (desired == DesiredSell.None)
            {
                await _clearOpenOrdersBlock
                    .ClearOpenOrdersAsync(symbol, OrderSide.Sell, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _ensureSingleOrderBlock
                    .EnsureSingleOrderAsync(symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, desired.Quantity, desired.Price, redeemSavings, cancellationToken)
                    .ConfigureAwait(false);
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
                candidateSellPrice = Math.Ceiling(candidateSellPrice / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;

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

            // break if the quantity is under the minimum lot size
            if (quantity < symbol.Filters.LotSize.StepSize)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, symbol.Filters.LotSize.StepSize, symbol.BaseAsset);

                return DesiredSell.None;
            }

            // adjust the quantity down to the lot size filter
            quantity = Math.Floor(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;

            // check if the sell is under the minimum notional filter
            if (quantity * ticker.ClosePrice < symbol.Filters.MinNotional.MinNotional)
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