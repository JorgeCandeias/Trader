using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.TrackingBuy
{
    internal class TrackingBuyExecutor : IAlgoCommandExecutor<TrackingBuyCommand>
    {
        private readonly ILogger _logger;
        private readonly ITickerProvider _tickers;
        private readonly IBalanceProvider _balances;
        private readonly ISavingsProvider _savings;
        private readonly IOrderProvider _orders;

        public TrackingBuyExecutor(ILogger<TrackingBuyExecutor> logger, ITickerProvider tickers, IBalanceProvider balances, ISavingsProvider savings, IOrderProvider orders)
        {
            _logger = logger;
            _tickers = tickers;
            _balances = balances;
            _savings = savings;
            _orders = orders;
        }

        private static string TypeName => nameof(TrackingBuyExecutor);

        public async Task ExecuteAsync(IAlgoContext context, TrackingBuyCommand command, CancellationToken cancellationToken = default)
        {
            var ticker = await _tickers.GetRequiredTickerAsync(command.Symbol.Name, cancellationToken).ConfigureAwait(false);
            var orders = await _orders.GetTransientOrdersBySideAsync(command.Symbol.Name, OrderSide.Buy, cancellationToken).ConfigureAwait(false);
            var balance = await _balances.GetRequiredBalanceAsync(command.Symbol.QuoteAsset, cancellationToken).ConfigureAwait(false);
            var savings = await _savings.GetPositionOrZeroAsync(command.Symbol.QuoteAsset, cancellationToken).ConfigureAwait(false);

            // identify the free balance
            var free = balance.Free + (command.RedeemSavings ? savings.FreeAmount : 0m);

            // identify the target low price for the first buy
            var lowBuyPrice = ticker.ClosePrice * command.PullbackRatio;

            // under adjust the buy price to the tick size
            lowBuyPrice = Math.Floor(lowBuyPrice / command.Symbol.Filters.Price.TickSize) * command.Symbol.Filters.Price.TickSize;

            _logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                TypeName, command.Symbol.Name, lowBuyPrice, command.Symbol.QuoteAsset, ticker.ClosePrice, command.Symbol.QuoteAsset);

            orders = await TryCloseLowBuysAsync(context, command.Symbol, orders, lowBuyPrice, cancellationToken).ConfigureAwait(false);

            orders = await TryCloseHighBuysAsync(context, command.Symbol, orders, cancellationToken).ConfigureAwait(false);

            // if there are still open orders then leave them be
            if (orders.Count > 0) return;

            // calculate the target notional
            var total = free * command.TargetQuoteBalanceFractionPerBuy;

            // cap it at the max notional
            if (command.MaxNotional.HasValue)
            {
                total = Math.Min(total, command.MaxNotional.Value);
            }

            // bump it to the minimum notional if needed
            total = Math.Max(total, command.Symbol.Filters.MinNotional.MinNotional);

            // calculate the appropriate quantity to buy
            var quantity = total / lowBuyPrice;

            // round it down to the lot size step
            quantity = Math.Ceiling(quantity / command.Symbol.Filters.LotSize.StepSize) * command.Symbol.Filters.LotSize.StepSize;

            // calculat the true notional after adjustments
            total = quantity * lowBuyPrice;

            // check if it still is under the max notional after adjustments - some assets have very high minimum notionals or lot sizes
            if (command.MaxNotional.HasValue && total > command.MaxNotional)
            {
                _logger.LogError(
                    "{Type} {Name} cannot place buy order with amount of {Total} {Quote} because it is above the configured maximum notional of {MaxNotional}",
                    TypeName, command.Symbol.Name, total, command.Symbol.QuoteAsset, command.MaxNotional);

                return;
            }

            // ensure there is enough quote asset for it
            if (total > free)
            {
                var necessary = total - free;

                _logger.LogWarning(
                    "{Type} {Name} must place order with amount of {Total} {Quote} but the free amount is only {Free} {Quote}",
                    TypeName, command.Symbol.Name, total, command.Symbol.QuoteAsset, free, command.Symbol.QuoteAsset);

                if (command.RedeemSavings)
                {
                    _logger.LogInformation(
                        "Will attempt to redeem the necessary {Necessary} {Quote} from savings...",
                        necessary, command.Symbol.QuoteAsset);

                    var result = await new RedeemSavingsCommand(command.Symbol.QuoteAsset, necessary)
                        .ExecuteAsync(context, cancellationToken)
                        .ConfigureAwait(false);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "{Type} {Name} redeemed {Quantity} {Asset} from savings to cover the necessary {Necessary} {Asset}",
                            TypeName, command.Symbol.Name, result.Redeemed, command.Symbol.QuoteAsset, necessary, command.Symbol.QuoteAsset);

                        return;
                    }
                    else
                    {
                        _logger.LogError(
                            "{Type} {Name} could not redeem the necessary {Quantity} {Asset} from savings",
                            TypeName, command.Symbol.Name, necessary, command.Symbol.QuoteAsset);

                        return;
                    }
                }
            }

            _logger.LogInformation(
                "{Type} {Name} placing {OrderType} {OrderSode} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                TypeName, command.Symbol.Name, OrderType.Limit, OrderSide.Buy, command.Symbol.Name, quantity, command.Symbol.BaseAsset, lowBuyPrice, command.Symbol.QuoteAsset, quantity * lowBuyPrice, command.Symbol.QuoteAsset);

            // place the order now
            var tag = $"{command.Symbol.Name}{lowBuyPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);

            await new CreateOrderCommand(command.Symbol, OrderType.Limit, OrderSide.Buy, TimeInForce.GoodTillCanceled, quantity, lowBuyPrice, tag)
                .ExecuteAsync(context, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<OrderQueryResult>> TryCloseLowBuysAsync(IAlgoContext context, Symbol symbol, IReadOnlyList<OrderQueryResult> orders, decimal lowBuyPrice, CancellationToken cancellationToken)
        {
            // cancel all open buy orders with an open price lower than the lower band to the current price
            foreach (var order in orders.Where(x => x.Side == OrderSide.Buy && x.Price < lowBuyPrice))
            {
                _logger.LogInformation(
                    "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                    TypeName, symbol.Name, order.Price, order.OriginalQuantity);

                await new CancelOrderCommand(symbol, order.OrderId)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                orders = orders.ToImmutableList().Remove(order);
            }

            return orders;
        }

        private async Task<IReadOnlyList<OrderQueryResult>> TryCloseHighBuysAsync(IAlgoContext context, Symbol symbol, IReadOnlyList<OrderQueryResult> orders, CancellationToken cancellationToken)
        {
            foreach (var order in orders.Where(x => x.Side == OrderSide.Buy).OrderBy(x => x.Price).Skip(1))
            {
                _logger.LogInformation(
                    "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                    TypeName, symbol.Name, order.Price, order.OriginalQuantity);

                await new CancelOrderCommand(symbol, order.OrderId)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                orders = orders.ToImmutableList().Remove(order);
            }

            // let the algo resync if any orders where closed
            return orders;
        }
    }
}