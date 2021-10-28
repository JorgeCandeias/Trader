using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Providers;
using Outcompute.Trader.Trading.Providers.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms.Stepping
{
    internal class SteppingAlgo : SymbolAlgo
    {
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<SteppingAlgoOptions> _monitor;
        private readonly IOrderCodeGenerator _orderCodeGenerator;
        private readonly IOrderProvider _orderProvider;

        public SteppingAlgo(ILogger<SteppingAlgo> logger, IOptionsMonitor<SteppingAlgoOptions> options, IOrderCodeGenerator orderCodeGenerator, IOrderProvider orderProvider)
        {
            _logger = logger;
            _monitor = options;
            _orderCodeGenerator = orderCodeGenerator;
            _orderProvider = orderProvider;
        }

        private static string TypeName => nameof(SteppingAlgo);

        /// <summary>
        /// Options active for each execution.
        /// </summary>
        private SteppingAlgoOptions _options = new();

        /// <summary>
        /// Keeps track of the bands managed by the algorithm.
        /// </summary>
        private readonly SortedSet<Band> _bands = new();

        public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            // pin the options for this execution
            _options = _monitor.Get(Context.Name);

            // get the non-significant open buy orders to the bands
            var nonSignificantTransientBuyOrders = await _orderProvider
                .GetNonSignificantTransientOrdersBySideAsync(Context.Symbol.Name, OrderSide.Buy, cancellationToken)
                .ConfigureAwait(false);

            var transientSellOrders = await _orderProvider
                .GetTransientOrdersBySideAsync(Context.Symbol.Name, OrderSide.Sell, cancellationToken)
                .ConfigureAwait(false);

            var transientBuyOrders = await _orderProvider
                .GetTransientOrdersBySideAsync(Context.Symbol.Name, OrderSide.Buy, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "{Type} {Name} reports latest asset price is {Price} {QuoteAsset}",
                TypeName, Context.Name, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset);

            return
                TryCreateTradingBands(nonSignificantTransientBuyOrders, transientSellOrders) ??
                await TrySetStartingTradeAsync(transientBuyOrders, cancellationToken) ??
                TryCancelRogueSellOrders(transientSellOrders) ??
                TryCancelExcessSellOrders(transientSellOrders) ??
                await TrySetBandSellOrdersAsync(transientSellOrders, cancellationToken) ??
                await TryCreateLowerBandOrderAsync(cancellationToken) ??
                TryCloseOutOfRangeBands() ??
                Noop();
        }

        private IAlgoCommand? TryCloseOutOfRangeBands()
        {
            // take the upper band
            var upper = _bands.Max;
            if (upper is null) return null;

            // calculate the step size
            var step = upper.OpenPrice * _options.PullbackRatio;

            // take the lower band
            var band = _bands.Min;
            if (band is null) return null;

            // ensure the lower band is on ordered status
            if (band.Status != BandStatus.Ordered) return null;

            // ensure the lower band is opening within reasonable range of the current price
            if (band.OpenPrice >= Context.Ticker.ClosePrice - step) return null;

            // if the above checks fails then close the band
            if (band.OpenOrderId is not 0)
            {
                return CancelOrder(Context.Symbol, band.OpenOrderId);
            }

            return null;
        }

        private async ValueTask<IAlgoCommand?> TryCreateLowerBandOrderAsync(CancellationToken cancellationToken = default)
        {
            // identify the highest and lowest bands
            var highBand = _bands.Max;
            var lowBand = _bands.Min;

            if (lowBand is null || highBand is null)
            {
                _logger.LogError(
                    "{Type} {Name} attempted to create a new lower band without an existing band yet",
                    TypeName, Context.Name);

                // something went wrong so let the algo reset
                return Noop();
            }

            // skip if the current price is at or above the band open price
            if (Context.Ticker.ClosePrice >= lowBand.OpenPrice)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports price {Price} {Quote} is within the current low band of {OpenPrice} {Quote} to {ClosePrice} {Quote}",
                    TypeName, Context.Name, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset, lowBand.OpenPrice, Context.Symbol.QuoteAsset, lowBand.ClosePrice, Context.Symbol.QuoteAsset);

                // let the algo continue
                return null;
            }

            // skip if we are already at the maximum number of bands
            if (_bands.Count >= _options.MaxBands)
            {
                _logger.LogWarning(
                    "{Type} {Name} has reached the maximum number of {Count} bands",
                    TypeName, Context.Name, _options.MaxBands);

                // let the algo continue
                return null;
            }

            // skip if lower band creation is disabled
            if (!_options.IsLowerBandOpeningEnabled)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create lower band because the feature is disabled",
                    TypeName, Context.Name);

                return null;
            }

            // find the lower price under the current price and low band
            var lowerPrice = highBand.OpenPrice;
            var stepPrice = highBand.ClosePrice - highBand.OpenPrice;
            while (lowerPrice >= Context.Ticker.ClosePrice || lowerPrice >= lowBand.OpenPrice)
            {
                lowerPrice -= stepPrice;
            }

            // protect from weird stuff
            if (lowerPrice <= 0)
            {
                throw new AlgorithmException($"Somehow we got to a negative lower price of {lowerPrice}!");
            }

            // under adjust the buy price to the tick size
            lowerPrice = lowerPrice.AdjustPriceUpToTickSize(Context.Symbol);

            // calculate the quote amount to pay with
            var total = GetFreeBalance() * _options.TargetQuoteBalanceFractionPerBand;

            // lower below the max notional if needed
            if (_options.MaxNotional.HasValue)
            {
                total = Math.Min(total, _options.MaxNotional.Value);
            }

            // raise to the minimum notional if needed
            total = total.AdjustTotalUpToMinNotional(Context.Symbol);

            // ensure there is enough quote asset for it
            if (total > Context.QuoteSpotBalance.Free)
            {
                var necessary = total - Context.QuoteSpotBalance.Free;

                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}. Will attempt to redeem from savings...",
                    TypeName, Context.Name, total, Context.Symbol.QuoteAsset, Context.QuoteSpotBalance.Free, Context.Symbol.QuoteAsset);

                var result = await TryRedeemSavings(Context.Symbol.QuoteAsset, necessary)
                    .ExecuteAsync(Context, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "{Type} {Name} redeemed {Amount} {Asset} successfully",
                        TypeName, Context.Name, necessary, Context.Symbol.QuoteAsset);

                    // let the algo cycle to allow redemption to process
                    return Noop();
                }
                else
                {
                    _logger.LogError(
                        "{Type} {Name} failed to redeem the necessary amount of {Quantity} {Asset}",
                        TypeName, Context.Name, necessary, Context.Symbol.QuoteAsset);

                    return null;
                }
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowerPrice;

            // round it down to the lot size step
            quantity = quantity.AdjustQuantityUpToLotSize(Context.Symbol);

            // place the buy order
            var tag = $"{Context.Symbol.Name}{lowerPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
            return CreateOrder(Context.Symbol, OrderType.Limit, OrderSide.Buy, TimeInForce.GoodTillCanceled, quantity, lowerPrice, tag);
        }

        /// <summary>
        /// Sets sell orders for open bands that do not have them yet.
        /// </summary>
        private async ValueTask<IAlgoCommand?> TrySetBandSellOrdersAsync(IReadOnlyList<OrderQueryResult> transientSellOrders, CancellationToken cancellationToken = default)
        {
            // skip if we have reach the max sell orders
            if (transientSellOrders.Count >= _options.MaxActiveSellOrders)
            {
                return null;
            }

            // create a sell order for the lowest band only
            foreach (var band in _bands.Where(x => x.Status == BandStatus.Open).Take(_options.MaxActiveSellOrders))
            {
                if (band.CloseOrderId is 0)
                {
                    // acount for leftovers
                    if (band.Quantity > Context.AssetSpotBalance.Free)
                    {
                        var necessary = band.Quantity - Context.AssetSpotBalance.Free;

                        if (_options.RedeemAssetSavings)
                        {
                            _logger.LogInformation(
                                "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available. Will attempt to redeem {Necessary} {Asset} rest from savings.",
                                TypeName, Context.Name, OrderType.Limit, OrderSide.Sell, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free, Context.Symbol.BaseAsset, necessary, Context.Symbol.BaseAsset);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available and savings redemption is disabled.",
                                TypeName, Context.Name, OrderType.Limit, OrderSide.Sell, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free);

                            return null;
                        }

                        var result = await TryRedeemSavings(Context.Symbol.BaseAsset, necessary)
                            .ExecuteAsync(Context, cancellationToken)
                            .ConfigureAwait(false);

                        if (result.Success)
                        {
                            _logger.LogInformation(
                                "{Type} {Name} redeemed {Amount} {Asset} successfully",
                                TypeName, Context.Name, necessary, Context.Symbol.BaseAsset);

                            // let the algo cycle to allow redemption to process
                            return Noop();
                        }
                        else
                        {
                            _logger.LogError(
                               "{Type} {Name} cannot set band sell order of {Quantity} {Asset} for {Price} {Quote} because there are only {Balance} {Asset} free and savings redemption failed",
                                TypeName, Context.Name, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free, Context.Symbol.BaseAsset);

                            return null;
                        }
                    }

                    var tag = _orderCodeGenerator.GetSellClientOrderId(band.OpenOrderId);

                    return CreateOrder(Context.Symbol, OrderType.Limit, OrderSide.Sell, TimeInForce.GoodTillCanceled, band.Quantity, band.ClosePrice, tag);
                }
            }

            return null;
        }

        /// <summary>
        /// Identify and cancel rogue sell orders that do not belong to a trading band.
        /// </summary>
        private IAlgoCommand? TryCancelRogueSellOrders(IReadOnlyList<OrderQueryResult> transientSellOrders)
        {
            foreach (var orderId in transientSellOrders.Select(x => x.OrderId))
            {
                if (!_bands.Any(x => x.CloseOrderId == orderId))
                {
                    return CancelOrder(Context.Symbol, orderId);
                }
            }

            return null;
        }

        /// <summary>
        /// Identify and cancel excess sell orders above the limit.
        /// </summary>
        private IAlgoCommand? TryCancelExcessSellOrders(IReadOnlyList<OrderQueryResult> transientSellOrders)
        {
            // get the order ids for the lowest open bands
            var bands = _bands
                .Where(x => x.Status == BandStatus.Open)
                .Take(_options.MaxActiveSellOrders)
                .Select(x => x.CloseOrderId)
                .Where(x => x is not 0)
                .ToHashSet();

            // cancel all excess sell orders now
            foreach (var orderId in transientSellOrders.Select(x => x.OrderId))
            {
                if (!bands.Contains(orderId))
                {
                    return CancelOrder(Context.Symbol, orderId);
                }
            }

            return null;
        }

        private decimal GetFreeBalance()
        {
            return Context.QuoteSpotBalance.Free + (_options.UseQuoteSavings ? Context.QuoteSavingsBalance.FreeAmount : 0m);
        }

        private async ValueTask<IAlgoCommand?> TrySetStartingTradeAsync(IReadOnlyList<OrderQueryResult> transientBuyOrders, CancellationToken cancellationToken = default)
        {
            // disable the opening if required
            if (!_options.IsOpeningEnabled)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create the opening band because it is disabled",
                    TypeName, Context.Name);

                return ClearOpenOrders(OrderSide.Buy);
            }

            // skip if there is more than one band
            if (_bands.Count > 1)
            {
                return null;
            }

            // skip if there is one band but it is already active
            if (_bands.Count == 1 && _bands.Min!.Status != BandStatus.Ordered)
            {
                return null;
            }

            // identify the target low price for the first buy
            var lowBuyPrice = Context.Ticker.ClosePrice;

            // under adjust the buy price to the tick size
            lowBuyPrice = lowBuyPrice.AdjustPriceDownToTickSize(Context.Symbol);

            _logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                TypeName, Context.Name, lowBuyPrice, Context.Symbol.QuoteAsset, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset);

            // cancel the lowest open buy order with a open price lower than the lower band to the current price
            var lowest = transientBuyOrders.FirstOrDefault(x => x.Side == OrderSide.Buy && x.Status.IsTransientStatus());
            if (lowest is not null && lowest.Price < lowBuyPrice)
            {
                return CancelOrder(Context.Symbol, lowest.OrderId);
            }

            // calculate the amount to pay with
            var total = Math.Round(GetFreeBalance() * _options.TargetQuoteBalanceFractionPerBand, Context.Symbol.QuoteAssetPrecision);

            // lower below the max notional if needed
            if (_options.MaxNotional.HasValue)
            {
                total = Math.Min(total, _options.MaxNotional.Value);
            }

            // raise to the minimum notional if needed
            total = total.AdjustTotalUpToMinNotional(Context.Symbol);

            // ensure there is enough quote spot balance for it
            if (total > Context.QuoteSpotBalance.Free)
            {
                var necessary = total - Context.QuoteSpotBalance.Free;

                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}. Will attempt to redeem from savings...",
                    TypeName, Context.Name, total, Context.Symbol.QuoteAsset, Context.QuoteSpotBalance.Free, Context.Symbol.QuoteAsset);

                var result = await TryRedeemSavings(Context.Symbol.QuoteAsset, necessary)
                    .ExecuteAsync(Context, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "{Type} {Name} redeemed {Amount} {Asset} successfully",
                        TypeName, Context.Name, necessary, Context.Symbol.QuoteAsset);

                    // let the algo cycle to allow redemption to process
                    return Noop();
                }
                else
                {
                    _logger.LogError(
                        "{Type} {Name} failed to redeem the necessary amount of {Quantity} {Asset}",
                        TypeName, Context.Name, necessary, Context.Symbol.QuoteAsset);

                    return null;
                }
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowBuyPrice;

            // adjust the quantity up to the lot size
            quantity = quantity.AdjustQuantityUpToLotSize(Context.Symbol);

            // place a limit order at the current price
            var tag = $"{Context.Symbol.Name}{lowBuyPrice:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
            return CreateOrder(Context.Symbol, OrderType.Limit, OrderSide.Buy, TimeInForce.GoodTillCanceled, quantity, lowBuyPrice, tag);
        }

        private IAlgoCommand? ApplySignificantBuyOrdersToBands()
        {
            // apply the significant buy orders to the bands
            foreach (var order in Context.Significant.Orders.Where(x => x.Side == OrderSide.Buy))
            {
                if (order.Price is 0)
                {
                    _logger.LogError(
                        "{Type} {Name} identified a significant {OrderSide} {OrderType} order {OrderId} for {Quantity} {Asset} on {Time} with zero price and will let the algo refresh to pick up missing trades",
                        TypeName, Context.Name, order.Side, order.Type, order.OrderId, order.ExecutedQuantity, Context.Symbol.BaseAsset, order.Time);

                    return Noop();
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

            return null;
        }

        private IAlgoCommand? ApplyNonSignificantTransientBuyOrdersToBands(IReadOnlyList<OrderQueryResult> nonSignificantTransientBuyOrders)
        {
            foreach (var order in nonSignificantTransientBuyOrders)
            {
                if (order.Price is 0)
                {
                    _logger.LogError(
                        "{Type} {Name} identified a significant {OrderSide} {OrderType} order {OrderId} for {Quantity} {Asset} on {Time} with zero price and will let the algo refresh to pick up missing trades",
                        TypeName, Context.Name, order.Side, order.Type, order.OrderId, order.ExecutedQuantity, Context.Symbol.BaseAsset, order.Time);

                    return Noop();
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

            return null;
        }

        private void AdjustBandClosePrices()
        {
            // figure out the constant step size
            var stepSize = _bands.Max!.OpenPrice * _options.PullbackRatio;

            // adjust close prices on the bands
            foreach (var band in _bands)
            {
                band.ClosePrice = band.OpenPrice + stepSize;

                // ensure the close price is below the max percent filter
                // this can happen due to an asset crashing down several multiples
                var maxPrice = Context.Ticker.ClosePrice * Context.Symbol.Filters.PercentPrice.MultiplierUp;
                if (band.ClosePrice > maxPrice)
                {
                    _logger.LogError(
                        "{Type} {Name} detected band sell price for {Quantity} {Asset} of {Price} {Quote} is above the percent price filter of {MaxPrice} {Quote}",
                        TypeName, Context.Name, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, maxPrice, Context.Symbol.QuoteAsset);
                }

                // ensure the close price is above the min percent filter
                // this can happen to old leftovers that were bought very cheap
                var minPrice = Context.Ticker.ClosePrice * Context.Symbol.Filters.PercentPrice.MultiplierDown;
                if (band.ClosePrice < minPrice)
                {
                    _logger.LogWarning(
                        "{Type} {Name} adjusted sell of {Quantity} {Asset} for {ClosePrice} {Quote} to {MinPrice} {Quote} because it is below the percent price filter of {MinPrice} {Quote}",
                        TypeName, Context.Name, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, minPrice, Context.Symbol.QuoteAsset, minPrice, Context.Symbol.QuoteAsset);

                    band.ClosePrice = minPrice;
                }

                // adjust the sell price up to the tick size
                band.ClosePrice = Math.Ceiling(band.ClosePrice / Context.Symbol.Filters.Price.TickSize) * Context.Symbol.Filters.Price.TickSize;
            }
        }

        private void MergeLeftoverBands()
        {
            // skip this rule if there are not enough bands to evaluate
            if (_bands.Count < 2) return;

            // skip this rule if the lowest band is already ordered
            if (_bands.Min!.Status != BandStatus.Open) return;

            // keep merging the lowest band
            while (_bands.Count > 1)
            {
                // break if the lowest band is already above min lot size and min notional after adjustment
                if ((_bands.Min.Quantity.AdjustQuantityDownToLotSize(Context.Symbol) >= Context.Symbol.Filters.LotSize.MinQuantity) &&
                    (_bands.Min.Quantity.AdjustQuantityDownToLotSize(Context.Symbol) * _bands.Min.OpenPrice.AdjustPriceDownToTickSize(Context.Symbol)) >= Context.Symbol.Filters.MinNotional.MinNotional)
                {
                    break;
                }

                // get the lowest two bands
                var tail = _bands.Take(2).ToArray();
                var lowest = tail[0];
                var above = tail[1];

                // merge both bands
                var merged = new Band
                {
                    Status = BandStatus.Open,
                    Quantity = lowest.Quantity + above.Quantity,
                    OpenPrice = ((lowest.Quantity * lowest.OpenPrice) + (above.Quantity * above.OpenPrice)) / (lowest.Quantity + above.Quantity),
                    OpenOrderId = lowest.OpenOrderId,
                    CloseOrderId = lowest.CloseOrderId,
                    CloseOrderClientId = lowest.CloseOrderClientId
                };

                // remove current bands
                _bands.Remove(lowest);
                _bands.Remove(above);

                // add the new merged band
                _bands.Add(merged);
            }

            // adjust the lowest band so the order is valid
            _bands.Min.Quantity = _bands.Min.Quantity.AdjustQuantityDownToLotSize(Context.Symbol);
        }

        private IAlgoCommand? TryCreateTradingBands(IReadOnlyList<OrderQueryResult> nonSignificantTransientBuyOrders, IReadOnlyList<OrderQueryResult> transientSellOrders)
        {
            _bands.Clear();

            var result =
                ApplySignificantBuyOrdersToBands() ??
                ApplyNonSignificantTransientBuyOrdersToBands(nonSignificantTransientBuyOrders);

            if (result is not null)
            {
                return result;
            }

            // skip if no bands were created
            if (_bands.Count == 0) return null;

            MergeLeftoverBands();
            AdjustBandClosePrices();

            // apply open sell orders to the bands
            var used = new HashSet<Band>();

            foreach (var order in transientSellOrders)
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
                TypeName, Context.Name, _bands.Count, _bands);

            // always let the algo continue
            return null;
        }

        #region Classes

        private enum BandStatus
        {
            Ordered,
            Open
        }

        private sealed class Band : IComparable<Band>
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

        #endregion Classes
    }
}