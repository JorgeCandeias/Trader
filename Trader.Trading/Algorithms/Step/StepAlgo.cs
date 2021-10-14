using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms.Step
{
    internal class StepAlgo : IAlgo
    {
        private readonly IAlgoContext _context;

        private readonly ILogger _logger;
        private readonly IOptionsMonitor<StepAlgoOptions> _options;

        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;
        private readonly ISignificantOrderResolver _significantOrderResolver;
        private readonly ITradingRepository _repository;
        private readonly IOrderCodeGenerator _orderCodeGenerator;

        public StepAlgo(IAlgoContext context, ILogger<StepAlgo> logger, IOptionsMonitor<StepAlgoOptions> options, ISystemClock clock, ITradingService trader, ISignificantOrderResolver significantOrderResolver, ITradingRepository repository, IOrderCodeGenerator orderCodeGenerator)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _significantOrderResolver = significantOrderResolver ?? throw new ArgumentNullException(nameof(significantOrderResolver));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _orderCodeGenerator = orderCodeGenerator ?? throw new ArgumentNullException(nameof(orderCodeGenerator));
        }

        private static string TypeName => nameof(StepAlgo);

        /// <summary>
        /// Keeps track of the relevant account balances.
        /// </summary>
        private readonly Balances _balances = new();

        /// <summary>
        /// Keeps track of the bands managed by the algorithm.
        /// </summary>
        private readonly SortedSet<Band> _bands = new();

        public async ValueTask GoAsync(CancellationToken cancellationToken = default)
        {
            var options = _options.Get(_context.Name);

            var exchange = await _context
                .GetExchangeInfoAsync()
                .ConfigureAwait(false);

            var symbol = exchange.Symbols.Single(x => x.Name == options.Symbol);
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var percentFilter = symbol.Filters.OfType<PercentPriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            await ApplyAccountInfoAsync(symbol, cancellationToken).ConfigureAwait(false);

            var significant = await _significantOrderResolver
                .ResolveAsync(symbol, cancellationToken)
                .ConfigureAwait(false);

            await _context
                .PublishProfitAsync(significant.Profit)
                .ConfigureAwait(false);

            // always update the latest price
            var ticker = await TrySyncAssetPriceAsync(options, symbol, cancellationToken).ConfigureAwait(false);
            if (ticker is null) return;

            if (await TryCreateTradingBandsAsync(options, symbol, percentFilter, priceFilter, minNotionalFilter, significant.Orders, ticker, cancellationToken).ConfigureAwait(false)) return;
            if (await TrySetStartingTradeAsync(options, symbol, priceFilter, minNotionalFilter, lotSizeFilter, ticker, cancellationToken).ConfigureAwait(false)) return;
            if (await TryCancelRogueSellOrdersAsync(options, symbol, cancellationToken).ConfigureAwait(false)) return;
            if (await TryCancelExcessSellOrdersAsync(options, symbol, cancellationToken).ConfigureAwait(false)) return;
            if (await TrySetBandSellOrdersAsync(options, symbol, cancellationToken).ConfigureAwait(false)) return;
            if (await TryCreateLowerBandOrderAsync(options, symbol, priceFilter, minNotionalFilter, lotSizeFilter, ticker, cancellationToken).ConfigureAwait(false)) return;
            await TryCloseOutOfRangeBandsAsync(options, symbol, ticker, cancellationToken).ConfigureAwait(false);
        }

        private async Task ApplyAccountInfoAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            var assetBalance = await _repository
                .TryGetBalanceAsync(symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false) ??
                throw new AlgorithmException($"Could not get balance for base asset {symbol.BaseAsset}");

            _balances.Asset.Free = assetBalance.Free;
            _balances.Asset.Locked = assetBalance.Locked;

            _logger.LogInformation(
                "{Type} {Name} reports balance for base asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                TypeName, _context.Name, symbol.BaseAsset, _balances.Asset.Free, _balances.Asset.Locked, _balances.Asset.Total);

            var quoteBalance = await _repository
                .TryGetBalanceAsync(symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false) ??
                throw new AlgorithmException($"Could not get balance for quote asset {symbol.QuoteAsset}");

            _balances.Quote.Free = quoteBalance.Free;
            _balances.Quote.Locked = quoteBalance.Locked;

            _logger.LogInformation(
                "{Type} {Name} reports balance for quote asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                TypeName, _context.Name, symbol.QuoteAsset, _balances.Quote.Free, _balances.Quote.Locked, _balances.Quote.Total);
        }

        private async Task<MiniTicker?> TrySyncAssetPriceAsync(StepAlgoOptions options, Symbol symbol, CancellationToken cancellationToken = default)
        {
            var ticker = await _context.GetTickerProvider().TryGetTickerAsync(options.Symbol, cancellationToken).ConfigureAwait(false);

            if (ticker is not null)
            {
                _logger.LogInformation(
                "{Type} {Name} reports latest asset price is {Price} {QuoteAsset}",
                TypeName, _context.Name, ticker.ClosePrice, symbol.QuoteAsset);
            }

            return ticker;
        }

        private async Task<bool> TryCloseOutOfRangeBandsAsync(StepAlgoOptions options, Symbol symbol, MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            // take the upper band
            var upper = _bands.Max;
            if (upper is null) return false;

            // calculate the step size
            var step = upper.OpenPrice * options.PullbackRatio;

            // take the lower band
            var band = _bands.Min;
            if (band is null) return false;

            // ensure the lower band is on ordered status
            if (band.Status != BandStatus.Ordered) return false;

            // ensure the lower band is opening within reasonable range of the current price
            if (band.OpenPrice >= ticker.ClosePrice - step) return false;

            // if the above checks fails then close the band
            if (band.OpenOrderId is not 0)
            {
                var result = await _trader
                    .CancelOrderAsync(
                        new CancelStandardOrder(
                            options.Symbol,
                            band.OpenOrderId,
                            null,
                            null,
                            null,
                            _clock.UtcNow),
                        cancellationToken)
                    .ConfigureAwait(false);

                // save this order to the repository now to tolerate slow binance api updates
                await _repository
                    .SetOrderAsync(result, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "{Type} {Name} closed out-of-range {OrderSide} {OrderType} for {Quantity} {Asset} at {Price} {Quote}",
                    TypeName, _context.Name, result.Side, result.Type, result.OriginalQuantity, symbol.BaseAsset, result.Price, symbol.QuoteAsset);
            }

            return true;
        }

        private async Task<bool> TryCreateLowerBandOrderAsync(StepAlgoOptions options, Symbol symbol, PriceSymbolFilter priceFilter, MinNotionalSymbolFilter minNotionalFilter, LotSizeSymbolFilter lotSizeFilter, MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            // identify the highest and lowest bands
            var highBand = _bands.Max;
            var lowBand = _bands.Min;

            if (lowBand is null || highBand is null)
            {
                _logger.LogError(
                    "{Type} {Name} attempted to create a new lower band without an existing band yet",
                    TypeName, _context.Name);

                // something went wrong so let the algo reset
                return true;
            }

            // skip if the current price is at or above the band open price
            if (ticker.ClosePrice >= lowBand.OpenPrice)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports price {Price} {Quote} is within the current low band of {OpenPrice} {Quote} to {ClosePrice} {Quote}",
                    TypeName, _context.Name, ticker.ClosePrice, symbol.QuoteAsset, lowBand.OpenPrice, symbol.QuoteAsset, lowBand.ClosePrice, symbol.QuoteAsset);

                // let the algo continue
                return false;
            }

            // skip if we are already at the maximum number of bands
            if (_bands.Count >= options.MaxBands)
            {
                _logger.LogWarning(
                    "{Type} {Name} has reached the maximum number of {Count} bands",
                    TypeName, _context.Name, options.MaxBands);

                // let the algo continue
                return false;
            }

            // skip if lower band creation is disabled
            if (!options.IsLowerBandOpeningEnabled)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create lower band because the feature is disabled",
                    TypeName, _context.Name);

                return false;
            }

            // find the lower price under the current price and low band
            var lowerPrice = highBand.OpenPrice;
            var stepPrice = highBand.ClosePrice - highBand.OpenPrice;
            while (lowerPrice >= ticker.ClosePrice || lowerPrice >= lowBand.OpenPrice)
            {
                lowerPrice -= stepPrice;
            }

            // protect from weird stuff
            if (lowerPrice <= 0)
            {
                throw new AlgorithmException($"Somehow we got to a negative lower price of {lowerPrice}!");
            }

            // under adjust the buy price to the tick size
            lowerPrice = Math.Floor(lowerPrice / priceFilter.TickSize) * priceFilter.TickSize;

            // calculate the quote amount to pay with
            var total = _balances.Quote.Free * options.TargetQuoteBalanceFractionPerBand;

            // lower below the max notional if needed
            if (options.MaxNotional.HasValue)
            {
                total = Math.Min(total, options.MaxNotional.Value);
            }

            // raise to the minimum notional if needed
            total = Math.Max(total, minNotionalFilter.MinNotional);

            // ensure there is enough quote asset for it
            if (total > _balances.Quote.Free)
            {
                var necessary = total - _balances.Quote.Free;

                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}. Will attempt to redeem from savings...",
                    TypeName, _context.Name, total, symbol.QuoteAsset, _balances.Quote.Free, symbol.QuoteAsset);

                var (success, _) = await _context
                    .TryRedeemSavingsAsync(symbol.QuoteAsset, necessary, cancellationToken)
                    .ConfigureAwait(false);

                if (success)
                {
                    _logger.LogInformation(
                        "{Type} {Name} redeemed {Amount} {Asset} successfully",
                        TypeName, _context.Name, necessary, symbol.QuoteAsset);

                    // let the algo cycle to allow redemption to process
                    return true;
                }
                else
                {
                    _logger.LogError(
                        "{Type} {Name} failed to redeem the necessary amount of {Quantity} {Asset}",
                        TypeName, _context.Name, necessary, symbol.QuoteAsset);

                    return false;
                }
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowerPrice;

            // round it down to the lot size step
            quantity = Math.Ceiling(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // place the buy order
            var result = await _trader
                .CreateOrderAsync(
                    new Order(
                        options.Symbol,
                        OrderSide.Buy,
                        OrderType.Limit,
                        TimeInForce.GoodTillCanceled,
                        quantity,
                        null,
                        lowerPrice,
                        $"{options.Symbol}{lowerPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal),
                        null,
                        null,
                        NewOrderResponseType.Full,
                        null,
                        _clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);

            // save this order to the repository now to tolerate slow binance api updates
            await _repository
                .SetOrderAsync(result, 0m, 0m, 0m, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "{Type} {Name} placed {OrderType} {OrderSide} for {Quantity} {Asset} at {Price} {Quote}",
                TypeName, _context.Name, result.Type, result.Side, result.OriginalQuantity, symbol.BaseAsset, result.Price, symbol.QuoteAsset);

            return false;
        }

        /// <summary>
        /// Sets sell orders for open bands that do not have them yet.
        /// </summary>
        private async Task<bool> TrySetBandSellOrdersAsync(StepAlgoOptions options, Symbol symbol, CancellationToken cancellationToken = default)
        {
            // skip if we have reach the max sell orders
            var orders = await _repository
                .GetTransientOrdersBySideAsync(options.Symbol, OrderSide.Sell, cancellationToken)
                .ConfigureAwait(false);

            if (orders.Count >= options.MaxActiveSellOrders)
            {
                return false;
            }

            // create a sell order for the lowest band only
            foreach (var band in _bands.Where(x => x.Status == BandStatus.Open).Take(options.MaxActiveSellOrders))
            {
                if (band.CloseOrderId is 0)
                {
                    // acount for leftovers
                    if (band.Quantity > _balances.Asset.Free)
                    {
                        var necessary = band.Quantity - _balances.Asset.Free;

                        _logger.LogWarning(
                            "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available. Will attempt to redeem {Necessary} {Asset} rest from savings.",
                            TypeName, _context.Name, OrderType.Limit, OrderSide.Sell, band.Quantity, symbol.BaseAsset, band.ClosePrice, symbol.QuoteAsset, _balances.Asset.Free, symbol.BaseAsset, necessary, symbol.BaseAsset);

                        var (success, _) = await _context
                            .TryRedeemSavingsAsync(symbol.BaseAsset, necessary, cancellationToken)
                            .ConfigureAwait(false);

                        if (success)
                        {
                            _logger.LogInformation(
                                "{Type} {Name} redeemed {Amount} {Asset} successfully",
                                TypeName, _context.Name, necessary, symbol.BaseAsset);

                            // let the algo cycle to allow redemption to process
                            return true;
                        }
                        else
                        {
                            _logger.LogError(
                               "{Type} {Name} cannot set band sell order of {Quantity} {Asset} for {Price} {Quote} because there are only {Balance} {Asset} free and savings redemption failed",
                                TypeName, _context.Name, band.Quantity, symbol.BaseAsset, band.ClosePrice, symbol.QuoteAsset, _balances.Asset.Free, symbol.BaseAsset);

                            return false;
                        }
                    }

                    var result = await _trader
                        .CreateOrderAsync(
                            new Order(
                                options.Symbol,
                                OrderSide.Sell,
                                OrderType.Limit,
                                TimeInForce.GoodTillCanceled,
                                band.Quantity,
                                null,
                                band.ClosePrice,
                                _orderCodeGenerator.GetSellClientOrderId(band.OpenOrderId),
                                null,
                                null,
                                NewOrderResponseType.Full,
                                null,
                                _clock.UtcNow),
                            cancellationToken)
                        .ConfigureAwait(false);

                    // save this order to the repository now to tolerate slow binance api updates
                    await _repository
                        .SetOrderAsync(result, 0m, 0m, 0m, cancellationToken)
                        .ConfigureAwait(false);

                    band.CloseOrderId = result.OrderId;

                    _logger.LogInformation(
                        "{Type} {Name} placed {OrderType} {OrderSide} order for band of {Quantity} {Asset} with {OpenPrice} {Quote} at {ClosePrice} {Quote}",
                        TypeName, _context.Name, result.Type, result.Side, result.OriginalQuantity, symbol.BaseAsset, band.OpenPrice, symbol.QuoteAsset, result.Price, symbol.QuoteAsset);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Identify and cancel rogue sell orders that do not belong to a trading band.
        /// </summary>
        private async Task<bool> TryCancelRogueSellOrdersAsync(StepAlgoOptions options, Symbol symbol, CancellationToken cancellationToken = default)
        {
            // get all transient sell orders
            var orders = await _repository
                .GetTransientOrdersBySideAsync(options.Symbol, OrderSide.Sell, cancellationToken)
                .ConfigureAwait(false);

            var fail = false;

            foreach (var order in orders)
            {
                if (!_bands.Any(x => x.CloseOrderId == order.OrderId))
                {
                    // close the rogue sell order
                    var result = await _trader
                        .CancelOrderAsync(
                            new CancelStandardOrder(
                                options.Symbol,
                                order.OrderId,
                                null,
                                null,
                                null,
                                _clock.UtcNow),
                            cancellationToken)
                        .ConfigureAwait(false);

                    // save this order to the repository now to tolerate slow binance api updates
                    await _repository
                        .SetOrderAsync(result, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogWarning(
                        "{Type} {Name} cancelled sell order not associated with a band for {Quantity} {Asset} at {Price} {Quote}",
                        TypeName, _context.Name, result.OriginalQuantity, symbol.BaseAsset, result.Price, symbol.QuoteAsset);

                    fail = true;
                }
            }

            return fail;
        }

        /// <summary>
        /// Identify and cancel excess sell orders above the limit.
        /// </summary>
        private async Task<bool> TryCancelExcessSellOrdersAsync(StepAlgoOptions options, Symbol symbol, CancellationToken cancellationToken = default)
        {
            // get the order ids for the lowest open bands
            var bands = _bands
                .Where(x => x.Status == BandStatus.Open)
                .Take(options.MaxActiveSellOrders)
                .Select(x => x.CloseOrderId)
                .Where(x => x is not 0)
                .ToHashSet();

            // get all transient sell orders
            var orders = await _repository
                .GetTransientOrdersBySideAsync(options.Symbol, OrderSide.Sell, cancellationToken)
                .ConfigureAwait(false);

            // cancel all excess sell orders now
            var changed = false;
            foreach (var order in orders)
            {
                if (!bands.Contains(order.OrderId))
                {
                    // close the rogue sell order
                    var result = await _trader
                        .CancelOrderAsync(
                            new CancelStandardOrder(
                                options.Symbol,
                                order.OrderId,
                                null,
                                null,
                                null,
                                _clock.UtcNow),
                            cancellationToken)
                        .ConfigureAwait(false);

                    // save this order to the repository now to tolerate slow binance api updates
                    await _repository
                        .SetOrderAsync(result, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogWarning(
                        "{Type} {Name} cancelled excess sell order for {Quantity} {Asset} at {Price} {Quote}",
                        TypeName, _context.Name, result.OriginalQuantity, symbol.BaseAsset, result.Price, symbol.QuoteAsset);

                    changed = true;
                }
            }

            return changed;
        }

        private async Task<bool> TrySetStartingTradeAsync(StepAlgoOptions options, Symbol symbol, PriceSymbolFilter priceFilter, MinNotionalSymbolFilter minNotionalFilter, LotSizeSymbolFilter lotSizeFilter, MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            // only manage the opening if there are no bands or only a single order band to move around
            if (_bands.Count == 0 || _bands.Count == 1 && _bands.Single().Status == BandStatus.Ordered)
            {
                // identify the target low price for the first buy
                var lowBuyPrice = ticker.ClosePrice;

                // under adjust the buy price to the tick size
                lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

                _logger.LogInformation(
                    "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                    TypeName, _context.Name, lowBuyPrice, symbol.QuoteAsset, ticker.ClosePrice, symbol.QuoteAsset);

                // cancel the lowest open buy order with a open price lower than the lower band to the current price
                var orders = await _repository
                    .GetTransientOrdersBySideAsync(options.Symbol, OrderSide.Buy, cancellationToken)
                    .ConfigureAwait(false);

                var lowest = orders.FirstOrDefault(x => x.Side == OrderSide.Buy && x.Status.IsTransientStatus());
                if (lowest is not null)
                {
                    if (lowest.Price < lowBuyPrice)
                    {
                        var cancelled = await _trader
                            .CancelOrderAsync(
                                new CancelStandardOrder(
                                    options.Symbol,
                                    lowest.OrderId,
                                    null,
                                    null,
                                    null,
                                    _clock.UtcNow),
                                cancellationToken)
                            .ConfigureAwait(false);

                        // save this order to the repository now to tolerate slow binance api updates
                        await _repository
                            .SetOrderAsync(cancelled, cancellationToken)
                            .ConfigureAwait(false);

                        _logger.LogInformation(
                            "{Type} {Name} cancelled low starting open order with price {Price} for {Quantity} units",
                            TypeName, _context.Name, cancelled.Price, cancelled.OriginalQuantity);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "{Type} {Name} identified a closer opening order for {Quantity} {Asset} at {Price} {Quote} and will leave as-is",
                            TypeName, _context.Name, lowest.OriginalQuantity, symbol.BaseAsset, lowest.Price, symbol.QuoteAsset);
                    }

                    // let the algo resync
                    return true;
                }

                if (!options.IsOpeningEnabled)
                {
                    _logger.LogWarning(
                        "{Type} {Name} cannot create the opening band because it is disabled",
                        TypeName, _context.Name);

                    return true;
                }

                // calculate the amount to pay with
                var total = Math.Round(_balances.Quote.Free * options.TargetQuoteBalanceFractionPerBand, symbol.QuoteAssetPrecision);

                // lower below the max notional if needed
                if (options.MaxNotional.HasValue)
                {
                    total = Math.Min(total, options.MaxNotional.Value);
                }

                // raise to the minimum notional if needed
                total = Math.Max(total, minNotionalFilter.MinNotional);

                // ensure there is enough quote asset for it
                if (total > _balances.Quote.Free)
                {
                    var necessary = total - _balances.Quote.Free;

                    _logger.LogWarning(
                        "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}. Will attempt to redeem from savings...",
                        TypeName, _context.Name, total, symbol.QuoteAsset, _balances.Quote.Free, symbol.QuoteAsset);

                    var (success, _) = await _context
                        .TryRedeemSavingsAsync(symbol.QuoteAsset, necessary, cancellationToken)
                        .ConfigureAwait(false);

                    if (success)
                    {
                        _logger.LogInformation(
                            "{Type} {Name} redeemed {Amount} {Asset} successfully",
                            TypeName, _context.Name, necessary, symbol.QuoteAsset);

                        // let the algo cycle to allow redemption to process
                        return true;
                    }
                    else
                    {
                        _logger.LogError(
                            "{Type} {Name} failed to redeem the necessary amount of {Quantity} {Asset}",
                            TypeName, _context.Name, necessary, symbol.QuoteAsset);

                        return false;
                    }
                }

                // calculate the appropriate quantity to buy
                var quantity = total / lowBuyPrice;

                // round it up to the lot size step
                quantity = Math.Ceiling(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

                // place a limit order at the current price
                var result = await _trader
                    .CreateOrderAsync(
                        new Order(
                            options.Symbol,
                            OrderSide.Buy,
                            OrderType.Limit,
                            TimeInForce.GoodTillCanceled,
                            quantity,
                            null,
                            lowBuyPrice,
                            $"{options.Symbol}{lowBuyPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal),
                            null,
                            null,
                            NewOrderResponseType.Full,
                            null,
                            _clock.UtcNow),
                        cancellationToken)
                    .ConfigureAwait(false);

                // save this order to the repository now to tolerate slow binance api updates
                await _repository
                    .SetOrderAsync(result, 0m, 0m, 0m, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "{Type} {Name} created {OrderSide} {OrderType} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                    TypeName, _context.Name, result.Side, result.Type, result.Symbol, result.OriginalQuantity, symbol.BaseAsset, result.Price, symbol.QuoteAsset, result.OriginalQuantity * result.Price, symbol.QuoteAsset);

                // skip the rest of this tick to let the algo resync
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> TryCreateTradingBandsAsync(StepAlgoOptions options, Symbol symbol, PercentPriceSymbolFilter percentFilter, PriceSymbolFilter priceFilter, MinNotionalSymbolFilter minNotionalFilter, ImmutableSortedOrderSet significant, MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            _bands.Clear();

            // apply the significant buy orders to the bands
            foreach (var order in significant.Where(x => x.Side == OrderSide.Buy))
            {
                if (order.Price is 0)
                {
                    _logger.LogError(
                        "{Type} {Name} identified a significant {OrderSide} {OrderType} order {OrderId} for {Quantity} {Asset} on {Time} with zero price and will let the algo refresh to pick up missing trades",
                        TypeName, _context.Name, order.Side, order.Type, order.OrderId, order.ExecutedQuantity, symbol.BaseAsset, order.Time);

                    return true;
                }

                if (order.Status.IsTransientStatus())
                {
                    // add transient orders with original quantity
                    _bands.Add(new Band
                    {
                        Quantity = order.OriginalQuantity,
                        OpenPrice = order.Price,
                        OpenOrderId = order.OrderId,
                        CloseOrderClientId = _orderCodeGenerator.GetSellClientOrderId(order.OrderId),
                        Status = BandStatus.Ordered
                    });
                }
                else
                {
                    // add completed orders with executed quantity
                    _bands.Add(new Band
                    {
                        Quantity = order.ExecutedQuantity,
                        OpenPrice = order.Price,
                        OpenOrderId = order.OrderId,
                        CloseOrderClientId = _orderCodeGenerator.GetSellClientOrderId(order.OrderId),
                        Status = BandStatus.Open
                    });
                }
            }

            // apply the non-significant open buy orders to the bands
            var orders = await _repository
                .GetNonSignificantTransientOrdersBySideAsync(options.Symbol, OrderSide.Buy, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                if (order.Price is 0)
                {
                    _logger.LogError(
                        "{Type} {Name} identified a significant {OrderSide} {OrderType} order {OrderId} for {Quantity} {Asset} on {Time} with zero price and will let the algo refresh to pick up missing trades",
                        TypeName, _context.Name, order.Side, order.Type, order.OrderId, order.ExecutedQuantity, symbol.BaseAsset, order.Time);

                    return true;
                }

                // add transient orders with original quantity
                _bands.Add(new Band
                {
                    Quantity = order.OriginalQuantity,
                    OpenPrice = order.Price,
                    OpenOrderId = order.OrderId,
                    CloseOrderClientId = _orderCodeGenerator.GetSellClientOrderId(order.OrderId),
                    Status = BandStatus.Ordered
                });
            }

            // skip if no bands were created
            if (_bands.Count == 0) return false;

            // figure out the constant step size
            var stepSize = _bands.Max!.OpenPrice * options.PullbackRatio;

            // adjust close prices on the bands
            foreach (var band in _bands)
            {
                band.ClosePrice = band.OpenPrice + stepSize;

                // ensure the close price is below the max percent filter
                // this can happen due to an asset crashing down several multiples
                var maxPrice = ticker.ClosePrice * percentFilter.MultiplierUp;
                if (band.ClosePrice > maxPrice)
                {
                    _logger.LogError(
                        "{Type} {Name} detected band sell price for {Quantity} {Asset} of {Price} {Quote} is above the percent price filter of {MaxPrice} {Quote}",
                        TypeName, _context.Name, band.Quantity, symbol.BaseAsset, band.ClosePrice, symbol.QuoteAsset, maxPrice, symbol.QuoteAsset);

                    // todo: this will raise an error later on so we need to handle this better
                }

                // ensure the close price is above the min percent filter
                // this can happen to old leftovers that were bought very cheap
                var minPrice = ticker.ClosePrice * percentFilter.MultiplierDown;
                if (band.ClosePrice < minPrice)
                {
                    _logger.LogWarning(
                        "{Type} {Name} adjusted sell of {Quantity} {Asset} for {ClosePrice} {Quote} to {MinPrice} {Quote} because it is below the percent price filter of {MinPrice} {Quote}",
                        TypeName, _context.Name, band.Quantity, symbol.BaseAsset, band.ClosePrice, symbol.QuoteAsset, minPrice, symbol.QuoteAsset, minPrice, symbol.QuoteAsset);

                    band.ClosePrice = minPrice;
                }

                // adjust the sell price up to the tick size
                band.ClosePrice = Math.Ceiling(band.ClosePrice / priceFilter.TickSize) * priceFilter.TickSize;
            }

            // identify bands where the target sell is somehow below the notional filter
            var leftovers = _bands.Where(x => x.Status == BandStatus.Open && x.Quantity * x.ClosePrice < minNotionalFilter.MinNotional).ToHashSet();
            if (leftovers.Count > 0)
            {
                // remove all leftovers
                foreach (var band in leftovers)
                {
                    _bands.Remove(band);
                }

                var quantity = leftovers.Sum(x => x.Quantity);
                var openPrice = leftovers.Sum(x => x.OpenPrice * x.Quantity) / leftovers.Sum(x => x.Quantity);
                var buyNotional = quantity * openPrice;
                var nowNotional = quantity * ticker.ClosePrice;

                _logger.LogWarning(
                    "{Type} {Name} ignoring {Count} under notional bands of {Quantity:N8} {Asset} bought at {BuyNotional:N8} {Quote} now worth {NowNotional:N8} {Quote} ({Percent:P2})",
                    TypeName,
                    _context.Name,
                    leftovers.Count,
                    leftovers.Sum(x => x.Quantity),
                    symbol.BaseAsset,
                    buyNotional,
                    symbol.QuoteAsset,
                    nowNotional,
                    symbol.QuoteAsset,
                    buyNotional > 0 ? nowNotional / buyNotional : 0);
            }

            // apply open sell orders to the bands
            var used = new HashSet<Band>();

            orders = await _repository
                .GetTransientOrdersBySideAsync(options.Symbol, OrderSide.Sell, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                var band = _bands.Except(used).SingleOrDefault(x => x.CloseOrderClientId == order.ClientOrderId);
                if (band is not null)
                {
                    band.CloseOrderId = order.OrderId;
                    used.Add(band);
                }
            }

            _logger.LogInformation(
                "{Type} {Name} is managing {Count} bands",
                TypeName, _context.Name, _bands.Count, _bands);

            // always let the algo continue
            return false;
        }

        #region Classes

        private enum BandStatus
        {
            Ordered,
            Open
        }

        private class Band : IComparable<Band>
        {
            public Guid Id { get; } = Guid.NewGuid();

            public long OpenOrderId { get; set; }

            public decimal Quantity { get; set; }
            public decimal OpenPrice { get; set; }
            public BandStatus Status { get; set; }
            public long CloseOrderId { get; set; }
            public decimal ClosePrice { get; set; }
            public string CloseOrderClientId { get; set; } = Empty;

            public int CompareTo(Band? other)
            {
                if (other is null) return 1;

                var byOpenPrice = OpenPrice.CompareTo(other.OpenPrice);
                if (byOpenPrice is not 0) return byOpenPrice;

                var byOpenOrderId = OpenOrderId.CompareTo(other.OpenOrderId);
                if (byOpenOrderId is not 0) return byOpenOrderId;

                var byId = Id.CompareTo(other.Id);
                if (byId is not 0) return byId;

                return 0;
            }

            public override bool Equals(object? obj) => obj is Band band && CompareTo(band) is 0;

            public override int GetHashCode() => HashCode.Combine(OpenPrice, Id);

            public static bool operator ==(Band first, Band second)
            {
                if (first is null) return second is null;

                return first.CompareTo(second) is 0;
            }

            public static bool operator !=(Band first, Band second)
            {
                if (first is null) return second is not null;

                return first.CompareTo(second) is 0;
            }

            public static bool operator <(Band first, Band second)
            {
                _ = first ?? throw new ArgumentNullException(nameof(first));
                _ = second ?? throw new ArgumentNullException(nameof(second));

                return first.CompareTo(second) < 0;
            }

            public static bool operator >(Band first, Band second)
            {
                _ = first ?? throw new ArgumentNullException(nameof(first));
                _ = second ?? throw new ArgumentNullException(nameof(second));

                return first.CompareTo(second) > 0;
            }

            public static bool operator <=(Band first, Band second)
            {
                _ = first ?? throw new ArgumentNullException(nameof(first));
                _ = second ?? throw new ArgumentNullException(nameof(second));

                return first.CompareTo(second) <= 0;
            }

            public static bool operator >=(Band first, Band second)
            {
                _ = first ?? throw new ArgumentNullException(nameof(first));
                _ = second ?? throw new ArgumentNullException(nameof(second));

                return first.CompareTo(second) >= 0;
            }
        }

        private class Balance
        {
            public decimal Free { get; set; }
            public decimal Locked { get; set; }
            public decimal Total => Free + Locked;
        }

        private class Balances
        {
            public Balance Asset { get; } = new();
            public Balance Quote { get; } = new();
        }

        #endregion Classes
    }
}