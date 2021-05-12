﻿using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;
using Trader.Models.Collections;

namespace Trader.Trading.Algorithms.Steps
{
    internal class TrackingBuyStep : ITrackingBuyStep
    {
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public TrackingBuyStep(ILogger<TrackingBuyStep> logger, ITradingRepository repository, ITradingService trader, ISystemClock clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Type => nameof(TrackingBuyStep);

        public Task<bool> GoAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return GoInnerAsync(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, cancellationToken);
        }

        private async Task<bool> GoInnerAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, CancellationToken cancellationToken)
        {
            // sync data from the exchange
            var orders = await GetOpenOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            // get the current price
            var ticker = await _repository
                .GetTickerAsync(symbol.Name, cancellationToken)
                .ConfigureAwait(false);

            // get the symbol filters
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            // get the account free quote balance
            var balance = await _repository
                .GetBalanceAsync(symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);

            // identify the target low price for the first buy
            var lowBuyPrice = ticker.ClosePrice * pullbackRatio;

            // under adjust the buy price to the tick size
            lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

            _logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                Type, symbol.Name, lowBuyPrice, symbol.QuoteAsset, ticker.ClosePrice, symbol.QuoteAsset);

            orders = await TryCloseLowBuysAsync(symbol, orders, lowBuyPrice, cancellationToken)
                .ConfigureAwait(false);

            orders = await TryCloseHighBuysAsync(symbol, orders, cancellationToken)
                .ConfigureAwait(false);

            // if there are still open orders then leave them be
            if (!orders.IsEmpty)
            {
                return false;
            }

            // calculate the amount to pay with
            var total = balance.Free * targetQuoteBalanceFractionPerBuy;

            // bump it to the minimum notional if needed
            total = Math.Max(total, minNotionalFilter.MinNotional);

            // ensure there is enough quote asset for it
            if (total > balance.Free)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                    Type, symbol.Name, total, symbol.QuoteAsset, balance.Free, symbol.QuoteAsset);

                return false;
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowBuyPrice;

            // round it down to the lot size step
            quantity = Math.Ceiling(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            _logger.LogInformation(
                "{Type} {Name} placing {OrderType} {OrderSode} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                Type, symbol.Name, OrderType.Limit, OrderSide.Buy, symbol.Name, quantity, symbol.BaseAsset, lowBuyPrice, symbol.QuoteAsset, quantity * lowBuyPrice, symbol.QuoteAsset);

            // place the order now
            var order = await _trader
                .CreateOrderAsync(
                    new Order(
                        symbol.Name,
                        OrderSide.Buy,
                        OrderType.Limit,
                        TimeInForce.GoodTillCanceled,
                        quantity,
                        null,
                        lowBuyPrice,
                        $"{symbol.Name}{lowBuyPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal),
                        null,
                        null,
                        NewOrderResponseType.Full,
                        null,
                        _clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);

            await _repository
                .SetOrderAsync(order, 0m, 0m, 0m, cancellationToken)
                .ConfigureAwait(false);

            return true;
        }

        private async Task<ImmutableSortedOrderSet> GetOpenOrdersAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            var orders = await _repository
                .GetTransientOrdersBySideAsync(symbol.Name, OrderSide.Buy, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                _logger.LogInformation(
                    "{Type} {Name} identified open {OrderSide} {OrderType} order for {Quantity} {Asset} at {Price} {Quote} totalling {Notional:N8} {Quote}",
                    Type, symbol.Name, order.Side, order.Type, order.OriginalQuantity, symbol.BaseAsset, order.Price, symbol.QuoteAsset, order.OriginalQuantity * order.Price, symbol.QuoteAsset);
            }

            return orders;
        }

        private async Task<ImmutableSortedOrderSet> TryCloseLowBuysAsync(Symbol symbol, ImmutableSortedOrderSet orders, decimal lowBuyPrice, CancellationToken cancellationToken)
        {
            // cancel all open buy orders with an open price lower than the lower band to the current price
            foreach (var order in orders.Where(x => x.Side == OrderSide.Buy && x.Price < lowBuyPrice))
            {
                _logger.LogInformation(
                    "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                    Type, symbol.Name, order.Price, order.OriginalQuantity);

                var result = await _trader
                    .CancelOrderAsync(
                        new CancelStandardOrder(
                            symbol.Name,
                            order.OrderId,
                            null,
                            null,
                            null,
                            _clock.UtcNow),
                        cancellationToken)
                    .ConfigureAwait(false);

                await _repository
                    .SetOrderAsync(result, cancellationToken)
                    .ConfigureAwait(false);

                orders = orders.Remove(order);
            }

            return orders;
        }

        private async Task<ImmutableSortedOrderSet> TryCloseHighBuysAsync(Symbol symbol, ImmutableSortedOrderSet orders, CancellationToken cancellationToken)
        {
            foreach (var order in orders.Where(x => x.Side == OrderSide.Buy).OrderBy(x => x.Price).Skip(1))
            {
                _logger.LogInformation(
                    "{Type} {Name} cancelling low starting open order with price {Price} for {Quantity} units",
                    Type, symbol.Name, order.Price, order.OriginalQuantity);

                var result = await _trader
                    .CancelOrderAsync(
                        new CancelStandardOrder(
                            symbol.Name,
                            order.OrderId,
                            null,
                            null,
                            null,
                            _clock.UtcNow),
                        cancellationToken)
                    .ConfigureAwait(false);

                await _repository
                    .SetOrderAsync(result, cancellationToken)
                    .ConfigureAwait(false);

                orders = orders.Remove(order);
            }

            // let the algo resync if any orders where closed
            return orders;
        }
    }
}