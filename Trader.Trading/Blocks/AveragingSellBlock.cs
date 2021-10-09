﻿using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    internal class AveragingSellBlock : IAveragingSellBlock
    {
        private readonly ILogger _logger;
        private readonly ISignificantOrderResolver _significantOrderResolver;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly IRedeemSavingsBlock _redeemSavingsBlock;
        private readonly ITickerProvider _tickers;
        private readonly ISavingsProvider _savings;

        public AveragingSellBlock(ILogger<AveragingSellBlock> logger, ISignificantOrderResolver significantOrderResolver, ITradingRepository repository, ITradingService trader, ISystemClock clock, IRedeemSavingsBlock redeemSavingsBlock, ITickerProvider tickers, ISavingsProvider savings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _significantOrderResolver = significantOrderResolver ?? throw new ArgumentNullException(nameof(significantOrderResolver));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _redeemSavingsBlock = redeemSavingsBlock ?? throw new ArgumentNullException(nameof(redeemSavingsBlock));
            _tickers = tickers ?? throw new ArgumentNullException(nameof(tickers));
            _savings = savings ?? throw new ArgumentNullException(nameof(savings));
        }

        private static string TypeName => nameof(AveragingSellBlock);

        public Task GoAsync(Symbol symbol, decimal profitMultiplier, bool useSavings, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return GoInnerAsync(symbol, profitMultiplier, useSavings, cancellationToken);
        }

        private async Task<Profit> GoInnerAsync(Symbol symbol, decimal profitMultiplier, bool useSavings, CancellationToken cancellationToken)
        {
            // get any required filters from the symbol
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var percentFilter = symbol.Filters.OfType<PercentPriceSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();

            // get all significant buys
            var significant = await _significantOrderResolver
                .ResolveAsync(symbol, cancellationToken)
                .ConfigureAwait(false);

            // get the current balance
            var balance = await _repository.TryGetBalanceAsync(symbol.BaseAsset, cancellationToken).ConfigureAwait(false)
                ?? Balance.Zero(symbol.BaseAsset);

            // get all savings if applicable
            var savings = (useSavings ? await _savings.TryGetFirstFlexibleProductPositionAsync(symbol.BaseAsset, cancellationToken).ConfigureAwait(false) : null)
                ?? FlexibleProductPosition.Zero(symbol.BaseAsset);

            // get the current ticker for the symbol
            var ticker = await _tickers
                .TryGetTickerAsync(symbol.Name, cancellationToken)
                .ConfigureAwait(false);

            // calculate the desired sell
            var desired = CalculateDesiredSell(symbol, profitMultiplier, significant.Orders, balance, savings, lotSizeFilter, percentFilter, ticker, priceFilter, minNotionalFilter);

            // remove all non-desired buy orders and set the desired sell order if needed
            await SetDesiredStateAsync(symbol, desired, balance, cancellationToken).ConfigureAwait(false);

            // return the latest known profit
            return significant.Profit;
        }

        private DesiredSell CalculateDesiredSell(Symbol symbol, decimal profitMultiplier, ImmutableSortedOrderSet orders, Balance balance, FlexibleProductPosition savings, LotSizeSymbolFilter lotSizeFilter, PercentPriceSymbolFilter percentFilter, MiniTicker? ticker, PriceSymbolFilter priceFilter, MinNotionalSymbolFilter minNotionalFilter)
        {
            // skip if there is no ticker information
            if (ticker is null)
            {
                _logger.LogWarning("{Type} cannot evaluate desired sell for symbol {Symbol} because no ticker information is yet available", TypeName, symbol.Name);

                return DesiredSell.None;
            }

            // skip if there is nothing to sell
            if (orders.IsEmpty)
            {
                return DesiredSell.None;
            }

            // take all known significant buy orders on the symbol
            var quantity = orders.Sum(x => x.ExecutedQuantity);

            // break if there is nothing to sell
            if (quantity <= 0m)
            {
                return DesiredSell.None;
            }

            // break if there are no assets to sell
            var total = balance.Free + savings.FreeAmount;
            if (total <= 0m)
            {
                _logger.LogWarning("{Type} cannot evaluate desired sell for symbol {Symbol} because there are not enough assets available to sell", TypeName, symbol.Name);

                return DesiredSell.None;
            }

            // detect the excess quantity available - gained interest etc
            var excess = total - quantity;
            if (excess < 0m)
            {
                _logger.LogWarning("{Type} cannot evaluate desired sell for symbol {Symbol} because the extra quantity is negative", TypeName, symbol.Name);

                return DesiredSell.None;
            }

            // calculate the weighted average price on all the significant orders
            var price = orders.Sum(x => x.Price * x.ExecutedQuantity);

            // if there is excess then add it at near zero cost
            if (excess > 0)
            {
                price += 0.00000001m * excess;
                quantity += excess;
            }

            price /= quantity;

            // bump the price by the profit multipler so we have a sell price
            price *= profitMultiplier;

            // adjust the quantity down to lot size filter
            if (quantity < lotSizeFilter.StepSize)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, lotSizeFilter.StepSize, symbol.BaseAsset);

                return DesiredSell.None;
            }
            quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // adjust the sell price up to the minimum percent filter
            var minPrice = ticker.ClosePrice * percentFilter.MultiplierDown;
            if (price < minPrice)
            {
                price = minPrice;
            }

            // adjust the sell price up to the tick size
            price = Math.Ceiling(price / priceFilter.TickSize) * priceFilter.TickSize;

            // check if the sell is under the minimum notional filter
            if (quantity * price < minNotionalFilter.MinNotional)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, minNotionalFilter.MinNotional, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // check if the sell is above the maximum percent filter
            if (price > ticker.ClosePrice * percentFilter.MultiplierUp)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the maximum percent filter price of {MaxPrice} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, ticker.ClosePrice * percentFilter.MultiplierUp, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // only sell if the price is at or above the ticker
            if (ticker.ClosePrice < price)
            {
                _logger.LogInformation(
                    "{Type} {Name} holding off sell order of {Quantity} {Asset} until price hits {Price} {Quote} ({Percent:P2} of current value of {Ticker} {Quote})",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, price / ticker.ClosePrice, ticker.ClosePrice, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // otherwise we now have a valid desired sell
            return new DesiredSell(quantity, price);
        }

        private async Task SetDesiredStateAsync(Symbol symbol, DesiredSell desired, Balance balance, CancellationToken cancellationToken)
        {
            var orders = await _repository
                .GetTransientOrdersBySideAsync(symbol.Name, OrderSide.Sell, cancellationToken)
                .ConfigureAwait(false);

            // cancel all non-desired orders
            foreach (var order in orders)
            {
                if (desired == DesiredSell.None || order.Type != OrderType.Limit || order.OriginalQuantity != desired.Quantity || order.Price != desired.Price)
                {
                    _logger.LogInformation(
                        "{Type} {Name} cancelling non-desired {OrderType} {OrderSide} order {OrderId} for {Quantity} {Asset} at {Price} {Quote}",
                        TypeName, symbol.Name, order.Type, order.Side, order.OrderId, order.OriginalQuantity, symbol.BaseAsset, order.Price, symbol.QuoteAsset);

                    var orderResult = await _trader
                        .CancelOrderAsync(
                            new CancelStandardOrder(
                                order.Symbol,
                                order.OrderId,
                                null,
                                null,
                                null,
                                _clock.UtcNow),
                            cancellationToken)
                        .ConfigureAwait(false);

                    await _repository
                        .SetOrderAsync(orderResult, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            // if any order survived then we can stop here
            if (!orders.IsEmpty) return;

            // if there is no desired sell then we can stop here
            if (desired == DesiredSell.None) return;

            var orderType = OrderType.Limit;
            var orderSide = OrderSide.Sell;

            // if there is not enough units to place the sell then attempt to redeem from savings
            if (balance.Free < desired.Quantity)
            {
                _logger.LogWarning(
                    "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available. Will attempt to redeem the rest from savings.",
                    TypeName, symbol.Name, orderType, orderSide, desired.Quantity, symbol.BaseAsset, desired.Price, symbol.QuoteAsset, balance.Free, symbol.BaseAsset);

                var necessary = desired.Quantity - balance.Free;

                var redeemed = await _redeemSavingsBlock
                    .GoAsync(symbol.BaseAsset, necessary, cancellationToken)
                    .ConfigureAwait(false);

                if (!redeemed)
                {
                    _logger.LogError(
                        "{Type} {Name} could not redeem the necessary {Quantity} {Asset} from savings",
                        TypeName, symbol.Name, necessary, symbol.BaseAsset);

                    return;
                }
            }

            // if there is no order left then we can set the desired sell

            _logger.LogInformation(
                "{Type} {Name} placing {OrderType} {OrderSide} order for {Quantity} {Asset} at {Price} {Quote}",
                TypeName, symbol.Name, orderType, orderSide, desired.Quantity, symbol.BaseAsset, desired.Price, symbol.QuoteAsset);

            var result = await _trader
                .CreateOrderAsync(
                    new Order(
                        symbol.Name,
                        orderSide,
                        orderType,
                        TimeInForce.GoodTillCanceled,
                        desired.Quantity,
                        null,
                        desired.Price,
                        $"{symbol.Name}{desired.Price:F8}".Replace(".", "", StringComparison.Ordinal),
                        null,
                        null,
                        NewOrderResponseType.Full,
                        null,
                        _clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);

            await _repository
                .SetOrderAsync(result, 0m, 0m, 0m, cancellationToken)
                .ConfigureAwait(false);
        }

        private record DesiredSell(decimal Quantity, decimal Price)
        {
            public static readonly DesiredSell None = new(0m, 0m);
        }
    }
}