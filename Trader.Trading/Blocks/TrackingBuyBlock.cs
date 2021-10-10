﻿using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    internal class TrackingBuyBlock : ITrackingBuyBlock
    {
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly IClearOpenBuyOrdersBlock _closeOpenBuyOrders;
        private readonly IRedeemSavingsBlock _redeemSavingsStep;
        private readonly ITickerProvider _tickers;
        private readonly IKlineProvider _klines;

        public TrackingBuyBlock(ILogger<TrackingBuyBlock> logger, ITradingRepository repository, ITradingService trader, ISystemClock clock, IClearOpenBuyOrdersBlock closeOpenBuyOrders, IRedeemSavingsBlock redeemSavingsStep, ITickerProvider tickers, IKlineProvider klines)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _closeOpenBuyOrders = closeOpenBuyOrders ?? throw new ArgumentNullException(nameof(closeOpenBuyOrders));
            _redeemSavingsStep = redeemSavingsStep ?? throw new ArgumentNullException(nameof(redeemSavingsStep));
            _tickers = tickers ?? throw new ArgumentNullException(nameof(tickers));
            _klines = klines ?? throw new ArgumentNullException(nameof(klines));
        }

        private static string TypeName => nameof(TrackingBuyBlock);

        public Task<bool> GoAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return GoInnerAsync(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, cancellationToken);
        }

        private async Task<bool> GoInnerAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, CancellationToken cancellationToken)
        {
            // get the current ticker for the symbol
            var ticker = await _tickers.TryGetTickerAsync(symbol.Name, cancellationToken).ConfigureAwait(false);

            if (ticker is null)
            {
                _logger.LogWarning(
                    "{Type} cannot evaluate desired sell for symbol {Symbol} because no ticker information is yet available",
                    TypeName, symbol.Name);

                return false;
            }

            // calculate safety averages
            var end = _clock.UtcNow;
            var start = end.Subtract(TimeSpan.FromDays(100));
            var klines = await _klines.GetKlinesAsync(symbol.Name, KlineInterval.Days1, start, end, cancellationToken).ConfigureAwait(false);
            var sma7 = klines.LastSimpleMovingAverage(x => x.ClosePrice, 7);
            var sma25 = klines.LastSimpleMovingAverage(x => x.ClosePrice, 25);
            var sma99 = klines.LastSimpleMovingAverage(x => x.ClosePrice, 99);

            _logger.LogInformation(
                "{Type} {Symbol} using indicators: (SMA7 = {SMA7}, SMA25 = {SMA25}, SMA99 = {SMA99})",
                TypeName, symbol.Name, sma7, sma25, sma99);

            if (ticker.ClosePrice > sma7 || ticker.ClosePrice > sma25 || ticker.ClosePrice > sma99)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected ticker is above the safety averages of ({SMA5}, {SMA25}, {SMA99}) and will not place a buy order",
                    TypeName, symbol.Name, sma7, sma25, sma99);

                await _closeOpenBuyOrders.GoAsync(symbol, cancellationToken).ConfigureAwait(false);

                return false;
            }

            // sync data from the exchange
            var orders = await GetOpenOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            // get the symbol filters

            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            // get the account free quote balance
            var balance = await _repository
                .TryGetBalanceAsync(symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);

            // identify the target low price for the first buy
            var lowBuyPrice = ticker.ClosePrice * pullbackRatio;

            // under adjust the buy price to the tick size
            lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

            _logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                TypeName, symbol.Name, lowBuyPrice, symbol.QuoteAsset, ticker.ClosePrice, symbol.QuoteAsset);

            orders = await TryCloseLowBuysAsync(symbol, orders, lowBuyPrice, cancellationToken)
                .ConfigureAwait(false);

            orders = await TryCloseHighBuysAsync(symbol, orders, cancellationToken)
                .ConfigureAwait(false);

            // if there are still open orders then leave them be
            if (!orders.IsEmpty)
            {
                return false;
            }

            // calculate the target notional
            var total = balance.Free * targetQuoteBalanceFractionPerBuy;

            // cap it at the max notional
            if (maxNotional.HasValue)
            {
                total = Math.Min(total, maxNotional.Value);
            }

            // bump it to the minimum notional if needed
            total = Math.Max(total, minNotionalFilter.MinNotional);

            // calculate the appropriate quantity to buy
            var quantity = total / lowBuyPrice;

            // round it down to the lot size step
            quantity = Math.Ceiling(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // calculat the true notional after adjustments
            total = quantity * lowBuyPrice;

            // check if it still is under the max notional after adjustments - some assets have very high minimum notionals or lot sizes
            if (maxNotional.HasValue && total > maxNotional)
            {
                _logger.LogError(
                    "{Type} {Name} cannot place buy order with amount of {Total} {Quote} because it is above the configured maximum notional of {MaxNotional}",
                    TypeName, symbol.Name, total, symbol.QuoteAsset, maxNotional);

                return false;
            }

            // ensure there is enough quote asset for it
            if (total > balance.Free)
            {
                var necessary = total - balance.Free;

                _logger.LogWarning(
                    "{Type} {Name} must place order with amount of {Total} {Quote} but the free amount is only {Free} {Quote}. Will attempt to redeem the necessary {Necessary} {Quote} from savings...",
                    TypeName, symbol.Name, total, symbol.QuoteAsset, balance.Free, symbol.QuoteAsset, necessary, symbol.QuoteAsset);

                var redeemed = await _redeemSavingsStep
                    .GoAsync(symbol.QuoteAsset, necessary, cancellationToken)
                    .ConfigureAwait(false);

                if (redeemed)
                {
                    _logger.LogInformation(
                        "{Type} {Name} redeemed {Quantity} {Asset} from savings",
                        TypeName, symbol.Name, necessary, symbol.QuoteAsset);

                    return true;
                }
                else
                {
                    _logger.LogError(
                        "{Type} {Name} could not redeem the necessary {Quantity} {Asset} from savings",
                        TypeName, symbol.Name, necessary, symbol.QuoteAsset);

                    return false;
                }
            }

            _logger.LogInformation(
                "{Type} {Name} placing {OrderType} {OrderSode} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                TypeName, symbol.Name, OrderType.Limit, OrderSide.Buy, symbol.Name, quantity, symbol.BaseAsset, lowBuyPrice, symbol.QuoteAsset, quantity * lowBuyPrice, symbol.QuoteAsset);

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
                    TypeName, symbol.Name, order.Side, order.Type, order.OriginalQuantity, symbol.BaseAsset, order.Price, symbol.QuoteAsset, order.OriginalQuantity * order.Price, symbol.QuoteAsset);
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
                    TypeName, symbol.Name, order.Price, order.OriginalQuantity);

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
                    TypeName, symbol.Name, order.Price, order.OriginalQuantity);

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