﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using static System.String;

namespace Trader.Trading.Algorithms.Step
{
    internal class StepAlgorithm : IStepAlgorithm
    {
        private readonly string _name;

        private readonly ILogger _logger;
        private readonly StepAlgorithmOptions _options;

        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;
        private readonly ISignificantOrderResolver _significantOrderResolver;
        private readonly ITraderRepository _repository;
        private readonly IOrderSynchronizer _orderSynchronizer;
        private readonly ITradeSynchronizer _tradeSynchronizer;
        private readonly IOrderCodeGenerator _orderCodeGenerator;

        public StepAlgorithm(string name, ILogger<StepAlgorithm> logger, IOptionsSnapshot<StepAlgorithmOptions> options, ISystemClock clock, ITradingService trader, ISignificantOrderResolver significantOrderResolver, ITraderRepository repository, IOrderSynchronizer orderSynchronizer, ITradeSynchronizer tradeSynchronizer, IOrderCodeGenerator orderCodeGenerator)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _significantOrderResolver = significantOrderResolver ?? throw new ArgumentNullException(nameof(significantOrderResolver));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _orderSynchronizer = orderSynchronizer ?? throw new ArgumentNullException(nameof(orderSynchronizer));
            _tradeSynchronizer = tradeSynchronizer ?? throw new ArgumentNullException(nameof(tradeSynchronizer));
            _orderCodeGenerator = orderCodeGenerator ?? throw new ArgumentNullException(nameof(orderCodeGenerator));
        }

        private static string Type => nameof(StepAlgorithm);

        public string Symbol => _options.Symbol;

        /// <summary>
        /// Keeps track of the relevant account balances.
        /// </summary>
        private readonly Balances _balances = new();

        /// <summary>
        /// Keeps track of the bands managed by the algorithm.
        /// </summary>
        private readonly SortedSet<Band> _bands = new();

        private Profit? _profit;

        public async Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var percentFilter = symbol.Filters.OfType<PercentPriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            ApplyAccountInfo(accountInfo);

            // synchronize the repository
            await _orderSynchronizer.SynchronizeOrdersAsync(_options.Symbol, cancellationToken);
            await _tradeSynchronizer.SynchronizeTradesAsync(_options.Symbol, cancellationToken);
            var significant = await _significantOrderResolver.ResolveAsync(_options.Symbol, cancellationToken);
            _profit = significant.Profit;

            // always update the latest price
            var ticker = await SyncAssetPriceAsync(cancellationToken);

            if (await TryCreateTradingBandsAsync(significant.Orders, minNotionalFilter, ticker, priceFilter, percentFilter, cancellationToken)) return;
            if (await TrySetStartingTradeAsync(symbol, ticker, lotSizeFilter, priceFilter, cancellationToken)) return;
            if (await TryCancelRogueSellOrdersAsync(cancellationToken)) return;
            if (await TrySetBandSellOrdersAsync(cancellationToken)) return;
            if (await TryCreateLowerBandOrderAsync(symbol, ticker, lotSizeFilter, priceFilter, cancellationToken)) return;
            await TryCloseOutOfRangeBandsAsync(ticker, cancellationToken);
        }

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profit ?? Profit.Zero);
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profit is null ? Statistics.Zero : Statistics.FromProfit(_profit));
        }

        private void ApplyAccountInfo(AccountInfo accountInfo)
        {
            var gotAsset = false;
            var gotQuote = false;

            foreach (var balance in accountInfo.Balances)
            {
                if (balance.Asset == _options.Asset)
                {
                    _balances.Asset.Free = balance.Free;
                    _balances.Asset.Locked = balance.Locked;

                    _logger.LogInformation(
                        "{Type} {Name} reports balance for base asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                        Type, _name, _options.Asset, balance.Free, balance.Locked, balance.Free + balance.Locked);

                    gotAsset = true;
                }
                else if (balance.Asset == _options.Quote)
                {
                    _balances.Quote.Free = balance.Free;
                    _balances.Quote.Locked = balance.Locked;

                    _logger.LogInformation(
                        "{Type} {Name} reports balance for quote asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                        Type, _name, _options.Quote, balance.Free, balance.Locked, balance.Free + balance.Locked);

                    gotQuote = true;
                }
            }

            if (!gotAsset) throw new AlgorithmException($"Could not get balance for base asset {_options.Asset}");
            if (!gotQuote) throw new AlgorithmException($"Could not get balance for quote asset {_options.Quote}");
        }

        private async Task<SymbolPriceTicker> SyncAssetPriceAsync(CancellationToken cancellationToken = default)
        {
            var ticker = await _trader.GetSymbolPriceTickerAsync(_options.Symbol, cancellationToken);

            _logger.LogInformation(
                "{Type} {Name} reports latest asset price is {Price} {QuoteAsset}",
                Type, _name, ticker.Price, _options.Quote);

            return ticker;
        }

        private async Task<bool> TryCloseOutOfRangeBandsAsync(SymbolPriceTicker ticker, CancellationToken cancellationToken = default)
        {
            // take the lower band
            var band = _bands.Min;
            if (band is null) return false;

            // ensure the lower band is on ordered status
            if (band.Status != BandStatus.Ordered) return false;

            // ensure the lower band is covering the current price
            if (band.OpenPrice < ticker.Price && band.ClosePrice > ticker.Price) return false;

            // if the above checks fails then close the band
            if (band.OpenOrderId is not 0)
            {
                var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, band.OpenOrderId, null, null, null, _clock.UtcNow), cancellationToken);

                _logger.LogInformation(
                    "{Type} {Name} closed out-of-range {OrderSide} {OrderType} for {Quantity} {Asset} at {Price} {Quote}",
                    Type, _name, result.Side, result.Type, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);
            }

            return true;
        }

        private async Task<bool> TryCreateLowerBandOrderAsync(Symbol symbol, SymbolPriceTicker ticker, LotSizeSymbolFilter lotSizeFilter, PriceSymbolFilter priceFilter, CancellationToken cancellationToken = default)
        {
            // identify the highest and lowest bands
            var highBand = _bands.Max;
            var lowBand = _bands.Min;

            if (lowBand is null || highBand is null)
            {
                _logger.LogError(
                    "{Type} {Name} attempted to create a new lower band without an existing band yet",
                    Type, _name);

                // something went wrong so let the algo reset
                return true;
            }

            // skip if the current price is at or above the band open price
            if (ticker.Price >= lowBand.OpenPrice)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports price {Price} {Quote} is within the current low band of {OpenPrice} {Quote} to {ClosePrice} {Quote}",
                    Type, _name, ticker.Price, _options.Quote, lowBand.OpenPrice, _options.Quote, lowBand.ClosePrice, _options.Quote);

                // let the algo continue
                return false;
            }

            // skip if we are already at the maximum number of bands
            if (_bands.Count >= _options.MaxBands)
            {
                _logger.LogWarning(
                    "{Type} {Name} has reached the maximum number of {Count} bands",
                    Type, _name, _options.MaxBands);

                // let the algo continue
                return false;
            }

            // find the lower price under the current price and low band
            var lowerPrice = highBand.OpenPrice;
            var stepPrice = highBand.ClosePrice - highBand.OpenPrice;
            while (lowerPrice >= ticker.Price || lowerPrice >= lowBand.OpenPrice)
            {
                lowerPrice -= stepPrice;
            }

            // protect some weird stuff
            if (lowerPrice <= 0)
            {
                throw new AlgorithmException($"Somehow we got to a negative lower price of {lowerPrice}!");
            }

            // under adjust the buy price to the tick size
            lowerPrice = Math.Floor(lowerPrice / priceFilter.TickSize) * priceFilter.TickSize;

            // calculate the amount to pay with
            var total = Math.Round(Math.Max(_balances.Quote.Free * _options.TargetQuoteBalanceFractionPerBand, _options.MinQuoteAssetQuantityPerOrder), symbol.QuoteAssetPrecision);

            // ensure there is enough quote asset for it
            if (total > _balances.Quote.Free)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                    Type, _name, total, _options.Quote, _balances.Quote.Free, _options.Quote);

                // there's no money for creating bands so let algo continue
                return false;
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowerPrice;

            // round it down to the lot size step
            quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // place the buy order
            var result = await _trader.CreateOrderAsync(new Order(_options.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, lowerPrice, null, null, null, NewOrderResponseType.Full, null, _clock.UtcNow), cancellationToken);

            _logger.LogInformation(
                "{Type} {Name} placed {OrderType} {OrderSide} for {Quantity} {Asset} at {Price} {Quote}",
                Type, _name, result.Type, result.Side, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);

            return false;
        }

        /// <summary>
        /// Sets sell orders for open bands that do not have them yet.
        /// </summary>
        private async Task<bool> TrySetBandSellOrdersAsync(CancellationToken cancellationToken = default)
        {
            foreach (var band in _bands.Where(x => x.Status == BandStatus.Open))
            {
                if (band.CloseOrderId is 0)
                {
                    // acount for leftovers
                    if (band.Quantity > _balances.Asset.Free)
                    {
                        _logger.LogError(
                            "{Type} {Name} cannot set band sell order of {Quantity} {Asset} for {Price} {Quote} because there are only {Balance} {Asset} free",
                            Type, _name, band.Quantity, _options.Asset, band.ClosePrice, _options.Quote, _balances.Asset.Free, _options.Asset);

                        return false;
                    }

                    var result = await _trader.CreateOrderAsync(
                        new Order(
                            _options.Symbol,
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
                        cancellationToken);

                    band.CloseOrderId = result.OrderId;

                    _logger.LogInformation(
                        "{Type} {Name} placed {OrderType} {OrderSide} order for band of {Quantity} {Asset} with {OpenPrice} {Quote} at {ClosePrice} {Quote}",
                        Type, _name, result.Type, result.Side, result.OriginalQuantity, _options.Asset, band.OpenPrice, _options.Quote, result.Price, _options.Quote);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Identify and cancel rogue sell orders that do not belong to a trading band.
        /// </summary>
        private async Task<bool> TryCancelRogueSellOrdersAsync(CancellationToken cancellationToken = default)
        {
            // get all transient sell orders
            var orders = await _repository.GetTransientOrdersAsync(_options.Symbol, OrderSide.Sell, null, cancellationToken);

            var fail = false;

            foreach (var order in orders)
            {
                if (!_bands.Any(x => x.CloseOrderId == order.OrderId))
                {
                    // close the rogue sell order
                    var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, order.OrderId, null, null, null, _clock.UtcNow), cancellationToken);

                    _logger.LogWarning(
                        "{Type} {Name} cancelled sell order not associated with a band for {Quantity} {Asset} at {Price} {Quote}",
                        Type, _name, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);

                    fail = true;
                }
            }

            return fail;
        }

        private async Task<bool> TrySetStartingTradeAsync(Symbol symbol, SymbolPriceTicker ticker, LotSizeSymbolFilter lotSizeFilter, PriceSymbolFilter priceFilter, CancellationToken cancellationToken = default)
        {
            // only manage the opening if there are no bands or only a single order band to move around
            if (_bands.Count == 0 || _bands.Count == 1 && _bands.Single().Status == BandStatus.Ordered)
            {
                // identify the target low price for the first buy
                var lowBuyPrice = ticker.Price * (1m - _options.PullbackRatio);

                // under adjust the buy price to the tick size
                lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

                _logger.LogInformation(
                    "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                    Type, _name, lowBuyPrice, _options.Quote, ticker.Price, _options.Quote);

                // cancel the lowest open buy order with a open price lower than the lower band to the current price
                var orders = await _repository.GetTransientOrdersAsync(_options.Symbol, OrderSide.Buy, null, cancellationToken);
                var lowest = orders.FirstOrDefault(x => x.Side == OrderSide.Buy && x.Status.IsTransientStatus());
                if (lowest is not null)
                {
                    if (lowest.Price < lowBuyPrice)
                    {
                        var cancelled = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, lowest.OrderId, null, null, null, _clock.UtcNow), cancellationToken);

                        _logger.LogInformation(
                            "{Type} {Name} cancelled low starting open order with price {Price} for {Quantity} units",
                            Type, _name, cancelled.Price, cancelled.OriginalQuantity);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "{Type} {Name} identified a closer opening order for {Quantity} {Asset} at {Price} {Quote} and will leave as-is",
                            Type, _name, lowest.OriginalQuantity, _options.Asset, lowest.Price, _options.Quote);
                    }

                    // let the algo resync
                    return true;
                }

                // calculate the amount to pay with
                var total = Math.Round(Math.Max(_balances.Quote.Free * _options.TargetQuoteBalanceFractionPerBand, _options.MinQuoteAssetQuantityPerOrder), symbol.QuoteAssetPrecision);

                // ensure there is enough quote asset for it
                if (total > _balances.Quote.Free)
                {
                    _logger.LogWarning(
                        "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                        Type, _name, total, _options.Quote, _balances.Quote.Free, _options.Quote);

                    return false;
                }

                // calculate the appropriate quantity to buy
                var quantity = total / ticker.Price;

                // round it down to the lot size step
                quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

                // place a market order to account for weird price floats
                var result = await _trader.CreateOrderAsync(new Order(
                    _options.Symbol,
                    OrderSide.Buy,
                    OrderType.Market,
                    null,
                    quantity,
                    null,
                    null,
                    null,
                    null,
                    null,
                    NewOrderResponseType.Full,
                    null,
                    _clock.UtcNow));

                // place a limit order at the current price
                /*
                var result = await _trader.CreateOrderAsync(new Order(
                    _options.Symbol,
                    OrderSide.Buy,
                    OrderType.Limit,
                    TimeInForce.GoodTillCanceled,
                    quantity,
                    null,
                    ticker.Price,
                    null,
                    null,
                    null,
                    NewOrderResponseType.Full,
                    null,
                    _clock.UtcNow),
                    cancellationToken);
                */

                _logger.LogInformation(
                    "{Type} {Name} created {OrderSide} {OrderType} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                    Type, _name, result.Side, result.Type, result.Symbol, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote, result.OriginalQuantity * result.Price, _options.Quote);

                // skip the rest of this tick to let the algo resync
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> TryCreateTradingBandsAsync(SortedOrderSet significant, MinNotionalSymbolFilter minNotionalFilter, SymbolPriceTicker ticker, PriceSymbolFilter priceFilter, PercentPriceSymbolFilter percentFilter, CancellationToken cancellationToken = default)
        {
            _bands.Clear();

            // apply the significant buy orders to the bands
            foreach (var order in significant.Where(x => x.Side == OrderSide.Buy))
            {
                if (order.Price is 0)
                {
                    _logger.LogError(
                        "{Type} {Name} identified a significant {OrderSide} {OrderType} order for {Quantity} {Asset} on {Time} with zero price and will let the algo refresh to pick up missing trades",
                        Type, _name, order.Side, order.Type, order.ExecutedQuantity, _options.Asset, order.Time);

                    return true;
                }

                if (order.Status.IsTransientStatus())
                {
                    // add transient orders with original quantity
                    _bands.Add(new Band
                    {
                        Quantity = order.OriginalQuantity,
                        ExecutedQuantity = order.ExecutedQuantity,
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
                        ExecutedQuantity = order.ExecutedQuantity,
                        OpenPrice = order.Price,
                        OpenOrderId = order.OrderId,
                        CloseOrderClientId = _orderCodeGenerator.GetSellClientOrderId(order.OrderId),
                        Status = BandStatus.Open
                    });
                }
            }

            // apply the non-significant open buy orders to the bands
            var orders = await _repository.GetTransientOrdersAsync(_options.Symbol, OrderSide.Buy, false, cancellationToken);
            foreach (var order in orders)
            {
                if (order.Price is 0)
                {
                    _logger.LogError(
                        "{Type} {Name} identified a significant {OrderSide} {OrderType} order for {Quantity} {Asset} on {Time} with zero price and will let the algo refresh to pick up missing trades",
                        Type, _name, order.Side, order.Type, order.ExecutedQuantity, _options.Asset, order.Time);

                    return true;
                }

                // add transient orders with original quantity
                _bands.Add(new Band
                {
                    Quantity = order.OriginalQuantity,
                    ExecutedQuantity = order.ExecutedQuantity,
                    OpenPrice = order.Price,
                    OpenOrderId = order.OrderId,
                    CloseOrderClientId = _orderCodeGenerator.GetSellClientOrderId(order.OrderId),
                    Status = BandStatus.Ordered
                });
            }

            // skip if no bands were created
            if (_bands.Count == 0) return false;

            // figure out the constant step size
            var stepSize = _bands.Max!.OpenPrice * _options.PullbackRatio;

            // adjust close prices on the bands
            foreach (var band in _bands)
            {
                band.ClosePrice = band.OpenPrice + stepSize;

                // ensure the close price is below the max percent filter
                var maxPrice = ticker.Price * percentFilter.MultiplierUp;
                if (band.ClosePrice > maxPrice)
                {
                    _logger.LogError(
                        "{Type} {Name} detected band sell price for {Quantity} {Asset} of {Price} {Quote} is above the percent price filter of {MaxPrice} {Quote}",
                        Type, _name, band.Quantity, _options.Asset, band.ClosePrice, _options.Quote, maxPrice, _options.Quote);

                    // todo: this will raise an error later on so we need to handle this better
                }

                // ensure the close price is above the min percent filter
                var minPrice = ticker.Price * percentFilter.MultiplierDown;
                if (band.ClosePrice < minPrice)
                {
                    _logger.LogWarning(
                        "{Type} {Name} adjusted sell of {Quantity} {Asset} for {ClosePrice} {Quote} to {MinPrice} {Quote} because it is below the percent price filter of {MinPrice} {Quote}",
                        Type, _name, band.Quantity, _options.Asset, band.ClosePrice, _options.Quote, minPrice, _options.Quote, minPrice, _options.Quote);

                    band.ClosePrice = minPrice;
                }

                // ensure the close price is not below the current price
                if (band.ClosePrice < ticker.Price)
                {
                    _logger.LogWarning(
                        "{Type} {Name} adjusted sell of {Quantity} {Asset} for {ClosePrice} {Quote} to {TickerPrice} {Quote} because it is below the ticker price of {TickerPrice} {Quote}",
                        Type, _name, band.Quantity, _options.Asset, band.ClosePrice, _options.Quote, ticker.Price, _options.Quote, ticker.Price, _options.Quote);

                    band.ClosePrice = ticker.Price;
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

                var openPrice = leftovers.Sum(x => x.OpenPrice * x.Quantity) / leftovers.Sum(x => x.Quantity);
                var closePrice = openPrice + stepSize;
                closePrice = Math.Ceiling(closePrice / priceFilter.TickSize) * priceFilter.TickSize;

                _logger.LogWarning(
                    "{Type} {Name} ignoring {Count} under notional bands with total {Quantity} {Asset}, avg opening at {OpenPrice:N2} {Quote}, target closing at {ClosePrice:N2} {Quote}",
                    Type, _name,
                    leftovers.Count,
                    leftovers.Sum(x => x.Quantity),
                    _options.Asset,
                    openPrice,
                    _options.Quote,
                    closePrice,
                    _options.Quote);
            }

            // apply open sell orders to the bands
            var used = new HashSet<Band>();
            orders = await _repository.GetTransientOrdersAsync(_options.Symbol, OrderSide.Sell, null, cancellationToken);
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
                Type, _name, _bands.Count, _bands);

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
            public decimal ExecutedQuantity { get; set; }
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
        }

        private class Balances
        {
            public Balance Asset { get; } = new();
            public Balance Quote { get; } = new();
        }

        #endregion Classes
    }
}