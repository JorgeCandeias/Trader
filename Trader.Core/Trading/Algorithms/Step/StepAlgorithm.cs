using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;

namespace Trader.Core.Trading.Algorithms.Step
{
    internal class StepAlgorithm : IStepAlgorithm
    {
        private readonly string _name;

        private readonly ILogger _logger;
        private readonly StepAlgorithmOptions _options;

        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;

        public StepAlgorithm(string name, ILogger<StepAlgorithm> logger, IOptionsSnapshot<StepAlgorithmOptions> options, ISystemClock clock, ITradingService trader)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private static string Type => nameof(StepAlgorithm);

        public string Symbol => _options.Symbol;

        private readonly CancellationTokenSource _cancellation = new();

        /// <summary>
        /// Set of trades synced from the trading service.
        /// </summary>
        private readonly SortedSet<AccountTrade> _trades = new(new AccountTradeIdComparer());

        /// <summary>
        /// Set of trades that compose the current asset balance.
        /// </summary>
        private readonly SortedSet<AccountTrade> _significant = new(new AccountTradeIdComparer());

        /// <summary>
        /// Descending set of open orders synced from the trading service.
        /// </summary>
        private readonly SortedSet<OrderQueryResult> _orders = new(new OrderQueryResultOrderIdComparer(false));

        private readonly SortedSet<Band> _bands = new();

        private Balances SyncAccountInfo(AccountInfo accountInfo)
        {
            _logger.LogInformation("{Type} {Name} querying account information...", Type, _name);

            var gotAsset = false;
            var gotQuote = false;

            var balances = new Balances();

            foreach (var balance in accountInfo.Balances)
            {
                if (balance.Asset == _options.Asset)
                {
                    balances.Asset.Free = balance.Free;
                    balances.Asset.Locked = balance.Locked;
                    gotAsset = true;

                    _logger.LogInformation(
                        "{Type} {Name} reports balance for base asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                        Type, _name, _options.Asset, balance.Free, balance.Locked, balance.Free + balance.Locked);
                }
                else if (balance.Asset == _options.Quote)
                {
                    balances.Quote.Free = balance.Free;
                    balances.Quote.Locked = balance.Locked;
                    gotQuote = true;

                    _logger.LogInformation(
                        "{Type} {Name} reports balance for quote asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                        Type, _name, _options.Quote, balance.Free, balance.Locked, balance.Free + balance.Locked);
                }
            }

            if (!gotAsset)
            {
                throw new AlgorithmException($"Could not get balance for base asset {_options.Asset}");
            }

            if (!gotQuote)
            {
                throw new AlgorithmException($"Could not get balance for quote asset {_options.Quote}");
            }

            return balances;
        }

        private async Task SyncAccountTradesAsync()
        {
            var trades = await _trader.GetAccountTradesAsync(new GetAccountTrades(_options.Symbol, null, null, _trades.Max?.Id + 1, 1000, null, _clock.UtcNow), _cancellation.Token);

            if (trades.Count > 0)
            {
                _trades.UnionWith(trades);

                _logger.LogInformation(
                    "{Type} {Name} got {Count} new trades from the exchange for a local total of {Total}",
                    Type, _name, trades.Count, _trades.Count);

                // remove redundant orders - this can happen when orders execute between api calls to orders and trades
                foreach (var trade in _trades)
                {
                    var removed = _orders.RemoveWhere(x => x.OrderId == trade.OrderId);
                    if (removed > 0)
                    {
                        _logger.LogWarning(
                            "{Type} {Name} removed {Count} redundant orders",
                            Type, _name, removed);
                    }
                }
            }
        }

        private async Task SyncAccountOpenOrdersAsync()
        {
            var orders = await _trader.GetOpenOrdersAsync(new GetOpenOrders(_options.Symbol, null, _clock.UtcNow));

            _orders.Clear();

            if (orders.Count > 0)
            {
                _orders.UnionWith(orders);

                _logger.LogInformation(
                    "{Type} {Name} got {Count} open orders from the exchange",
                    Type, _name, orders.Count);
            }
        }

        private async Task<SymbolPriceTicker> SyncAssetPriceAsync()
        {
            var ticker = await _trader.GetSymbolPriceTickerAsync(_options.Symbol, _cancellation.Token);

            _logger.LogInformation(
                "{Type} {Name} reports latest asset price is {Price} {QuoteAsset}",
                Type, _name, ticker.Price, _options.Quote);

            return ticker;
        }

        public async Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo)
        {
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            var balances = SyncAccountInfo(accountInfo);
            await SyncAccountOpenOrdersAsync();
            await SyncAccountTradesAsync();

            // always update the latest price
            var ticker = await SyncAssetPriceAsync();

            if (TryIdentifySignificantTrades(balances)) return;
            if (TrySyncTradingBands(priceFilter, minNotionalFilter)) return;
            if (await TrySetStartingTradeAsync(symbol, ticker, priceFilter, lotSizeFilter, balances)) return;
            if (await TryCancelRogueSellOrdersAsync()) return;
            if (await TrySetBandSellOrdersAsync()) return;
            if (await TryCreateLowerBandOrderAsync(symbol, ticker, priceFilter, lotSizeFilter, balances)) return;
            if (await TryCloseOutOfRangeBandsAsync(ticker, priceFilter)) return;
        }

        private async Task<bool> TryCloseOutOfRangeBandsAsync(SymbolPriceTicker ticker, PriceSymbolFilter priceFilter)
        {
            var threshold = ticker.Price / _options.TargetMultiplier;

            threshold = (threshold / priceFilter.TickSize) * priceFilter.TickSize;

            foreach (var band in _bands.Where(x => x.Status == BandStatus.Ordered && x.OpenPrice < threshold))
            {
                foreach (var orderId in band.OpenOrderIds)
                {
                    var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, orderId, null, null, null, _clock.UtcNow));

                    _logger.LogInformation(
                    "{Type} {Name} closed out-of-range {OrderSide} {OrderType} for {Quantity} {Asset} at {Price} {Quote}",
                    Type, _name, result.Side, result.Type, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);
                }

                return true;
            }

            return false;
        }

        private async Task<bool> TryCreateLowerBandOrderAsync(Symbol symbol, SymbolPriceTicker ticker, PriceSymbolFilter priceFilter, LotSizeSymbolFilter lotSizeFilter, Balances balances)
        {
            // identify the lowest band
            var lowBand = _bands.Min;
            if (lowBand is null)
            {
                _logger.LogError(
                    "{Type} {Name} attempted to create a new lower band without an existing band yet",
                    Type, _name);

                // something went wrong so stop the algo
                return true;
            }

            // skip if the current price is at or above the band open price
            if (ticker.Price >= lowBand.OpenPrice)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports price {Price} {Quote} is within the current band of {OpenPrice} {Quote} to {ClosePrice} {Quote} and will skip new band creation",
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

            // find the lower price under the current price
            var lowerPrice = lowBand.OpenPrice;
            while (lowerPrice > ticker.Price)
            {
                lowerPrice /= _options.TargetMultiplier;
            }

            // protect some weird stuff
            if (lowerPrice <= 0)
            {
                throw new AlgorithmException($"Somehow we got to a lower price of {lowerPrice}!");
            }

            // under adjust the buy price to the tick size
            lowerPrice = Math.Floor(lowerPrice / priceFilter.TickSize) * priceFilter.TickSize;

            // calculate the amount to pay with
            var total = Math.Round(Math.Max(balances.Quote.Free * _options.TargetQuoteBalanceFractionPerBand, _options.MinQuoteAssetQuantityPerOrder), symbol.QuoteAssetPrecision);

            // ensure there is enough quote asset for it
            if (total > balances.Quote.Free)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                    Type, _name, total, _options.Quote, balances.Quote.Free, _options.Quote);

                // there's no money for creating bands so let algo continue
                return false;
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowerPrice;

            // round it down to the lot size step
            quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // place the buy order
            var result = await _trader.CreateOrderAsync(new Order(_options.Symbol, OrderSide.Buy, OrderType.Limit, TimeInForce.GoodTillCanceled, quantity, null, lowerPrice, null, null, null, NewOrderResponseType.Full, null, _clock.UtcNow), _cancellation.Token);

            _logger.LogInformation(
                "{Type} {Name} placed {OrderType} {OrderSide} for {Quantity} {Asset} at {Price} {Quote}",
                Type, _name, result.Type, result.Side, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);

            return false;
        }

        /// <summary>
        /// Sets sell orders for open bands that do not have them yet.
        /// </summary>
        private async Task<bool> TrySetBandSellOrdersAsync()
        {
            var updated = false;

            foreach (var band in _bands.Where(x => x.Status == BandStatus.Open))
            {
                if (band.CloseOrderId is 0)
                {
                    var result = await _trader.CreateOrderAsync(new Order(_options.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, band.Quantity, null, band.ClosePrice, null, null, null, NewOrderResponseType.Full, null, _clock.UtcNow));

                    band.CloseOrderId = result.OrderId;

                    updated = true;

                    _logger.LogInformation(
                        "{Type} {Name} placed {OrderType} {OrderSide} order for band of {Quantity} {Asset} with {OpenPrice} {Quote} at {ClosePrice} {Quote}",
                        Type, _name, result.Type, result.Side, result.OriginalQuantity, _options.Asset, band.OpenPrice, _options.Quote, result.Price, _options.Quote);
                }
            }

            return updated;
        }

        /// <summary>
        /// Identify and cancel rogue sell orders that do not belong to a trading band.
        /// </summary>
        private async Task<bool> TryCancelRogueSellOrdersAsync()
        {
            var fail = false;

            foreach (var order in _orders.Where(x => x.Side == OrderSide.Sell))
            {
                if (!_bands.Any(x => x.CloseOrderId == order.OrderId))
                {
                    // close the rogue sell order
                    var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, order.OrderId, null, null, null, _clock.UtcNow));

                    _logger.LogWarning(
                        "{Type} {Name} cancelled sell order not associated with a band for {Quantity} {Asset} at {Price} {Quote}",
                        Type, _name, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);

                    fail = true;
                }
            }

            return fail;
        }

        private async Task<bool> TrySetStartingTradeAsync(Symbol symbol, SymbolPriceTicker ticker, PriceSymbolFilter priceFilter, LotSizeSymbolFilter lotSizeFilter, Balances balances)
        {
            // only manage the opening if there are no bands or only a single order band to move around
            if (_bands.Count == 0 || (_bands.Count == 1 && _bands.Single().Status == BandStatus.Ordered))
            {
                // cancel any rogue open sell orders - this should not be possible given balance is zero at this point
                /*
                foreach (var order in _orders)
                {
                    if (order.Side is OrderSide.Sell)
                    {
                        var cancelled = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, order.OrderId, null, null, null, _clock.UtcNow), _cancellation.Token);

                        _logger.LogWarning(
                            "{Type} {Name} cancelled rogue sell order at price {Price} for {Quantity} units",
                            Type, _name, cancelled.Price, cancelled.OriginalQuantity);

                        // skip the rest of this tick to let the algo resync
                        return true;
                    }
                }
                */

                /*
                // calculate the amount to pay with
                var total = Math.Round(Math.Max(_balances.Quote.Free * _options.TargetQuoteBalanceFractionPerBand, _options.MinQuoteAssetQuantityPerOrder), _parameters.Symbol.QuoteAssetPrecision);

                // adjust to price tick size
                total = Math.Floor(total / _parameters.PriceFilter.TickSize) * _parameters.PriceFilter.TickSize;

                // ensure there is enough quote asset for it
                if (total > _balances.Quote.Free)
                {
                    _logger.LogWarning(
                        "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                        Type, _name, total, _options.Quote, _balances.Quote.Free, _options.Quote);

                    return false;
                }

                var result = await _trader.CreateOrderAsync(new Order(
                    _options.Symbol,
                    OrderSide.Buy,
                    OrderType.Market,
                    null,
                    null,
                    total,
                    null,
                    null,
                    null,
                    null,
                    NewOrderResponseType.Full,
                    null,
                    _clock.UtcNow),
                    _cancellation.Token);

                _logger.LogInformation(
                    "{Type} {Name} created {OrderSide} {OrderType} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                    Type, _name, result.Side, result.Type, result.Symbol, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote, result.OriginalQuantity * result.Price, _options.Quote);

                return true;
                */

                // identify the target low price for the first buy
                var lowBuyPrice = ticker.Price / _options.TargetMultiplier;

                // under adjust the buy price to the tick size
                lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

                _logger.LogInformation(
                    "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                    Type, _name, lowBuyPrice, _options.Quote, ticker.Price, _options.Quote);

                // cancel all open buy orders with a open price lower than the lower band to the current price
                foreach (var order in _orders.Where(x => x.Side == OrderSide.Buy))
                {
                    if (order.Price < lowBuyPrice)
                    {
                        var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, order.OrderId, null, null, null, _clock.UtcNow));

                        _logger.LogInformation(
                            "{Type} {Name} cancelled low starting open order with price {Price} for {Quantity} units",
                            Type, _name, result.Price, result.OriginalQuantity);

                        _orders.Remove(order);

                        break;
                    }
                    else
                    {
                        _logger.LogInformation(
                            "{Type} {Name} identified a closer opening order for {Quantity} {Asset} at {Price} {Quote} and will leave as-is",
                            Type, _name, order.OriginalQuantity, _options.Asset, order.Price, _options.Quote);

                        return true;
                    }
                }

                // if there are still orders left then leave them be till the next tick
                if (_orders.Count > 0)
                {
                    return true;
                }
                else
                {
                    // put the starting order through

                    // calculate the amount to pay with
                    var total = Math.Round(Math.Max(balances.Quote.Free * _options.TargetQuoteBalanceFractionPerBand, _options.MinQuoteAssetQuantityPerOrder), symbol.QuoteAssetPrecision);

                    // ensure there is enough quote asset for it
                    if (total > balances.Quote.Free)
                    {
                        _logger.LogWarning(
                            "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                            Type, _name, total, _options.Quote, balances.Quote.Free, _options.Quote);

                        return false;
                    }

                    // calculate the appropriate quantity to buy
                    var quantity = total / lowBuyPrice;

                    // round it down to the lot size step
                    quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

                    var order = await _trader.CreateOrderAsync(new Order(
                        _options.Symbol,
                        OrderSide.Buy,
                        OrderType.Limit,
                        TimeInForce.GoodTillCanceled,
                        quantity,
                        null,
                        lowBuyPrice,
                        null,
                        null,
                        null,
                        NewOrderResponseType.Full,
                        null,
                        _clock.UtcNow),
                        _cancellation.Token);

                    _logger.LogInformation(
                        "{Type} {Name} created {OrderSide} {OrderType} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                        Type, _name, order.Side, order.Type, order.Symbol, order.OriginalQuantity, _options.Asset, order.Price, _options.Quote, order.OriginalQuantity * order.Price, _options.Quote);

                    // skip the rest of this tick to let the algo resync
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private bool TryIdentifySignificantTrades(Balances balances)
        {
            _significant.Clear();

            var total = balances.Asset.Total;

            if (total is 0)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports current asset value is zero and will not identify significant trades",
                    Type, _name);

                return false;
            }

            // keep track of the quantities for the pruning phase
            var remaining = new Dictionary<long, decimal>();

            foreach (var trade in _trades.Reverse())
            {
                if (trade.IsBuyer)
                {
                    // remove the buy trade from the total to bring it down to zero
                    total -= trade.Quantity;
                }
                else
                {
                    // add the sell trade to the total to move it away from zero
                    total += trade.Quantity;
                }

                // keep as a significant trade for now
                _significant.Add(trade);

                // keep track of the quantity for the pruning phase
                remaining[trade.Id] = trade.Quantity;

                // see if we got all the trades that matter
                if (total is 0) break;
            }

            // see if we got all trades
            /*
            if (total is not 0)
            {
                _logger.LogError(
                    "{Type} {Name} could not identify all significant trades that make up the current asset balance of {Total}",
                    Type, _name, balances.Asset.Total);

                return true;
            }
            */

            _logger.LogInformation(
                "{Type} {Name} identified {Count} significant trades that make up the asset balance of {Total}",
                Type, _name, _significant.Count, balances.Asset.Total);

            // now prune the significant trades to account interim sales
            var subjects = _significant.ToList();

            for (var i = 0; i < subjects.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects[i];
                if (!sell.IsBuyer)
                {
                    // loop through buys in lifo order
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = subjects[j];
                        if (buy.IsBuyer)
                        {
                            // remove as much as possible from the buy to satisfy the sell
                            var take = Math.Min(remaining[buy.Id], remaining[sell.Id]);
                            remaining[buy.Id] -= take;
                            remaining[sell.Id] -= take;
                        }
                    }
                }
            }

            // keep only buys with some quantity left
            _significant.Clear();
            foreach (var subject in subjects)
            {
                var quantity = remaining[subject.Id];
                if (subject.IsBuyer && quantity > 0)
                {
                    _significant.Add(new AccountTrade(subject.Symbol, subject.Id, subject.OrderId, subject.OrderListId, subject.Price, quantity, subject.QuoteQuantity, subject.Commission, subject.CommissionAsset, subject.Time, subject.IsBuyer, subject.IsMaker, subject.IsBestMatch));
                }
            }

            return false;
        }

        private bool TrySyncTradingBands(PriceSymbolFilter priceFilter, MinNotionalSymbolFilter minNotionalFilter)
        {
            _bands.Clear();

            // apply the significant buy trades to the bands
            foreach (var group in _significant.Where(x => x.IsBuyer).GroupBy(x => x.OrderId))
            {
                var band = new Band
                {
                    Quantity = group.Sum(x => x.Quantity),
                    OpenPrice = group.Sum(x => x.Price * x.Quantity) / group.Sum(x => x.Quantity),
                    ClosePrice = (group.Sum(x => x.Price * x.Quantity) / group.Sum(x => x.Quantity)) * _options.TargetMultiplier,
                    Status = BandStatus.Open
                };

                band.OpenOrderIds.Add(group.Key);

                // adjust the target price to the tick size
                band.ClosePrice = Math.Floor(band.ClosePrice / priceFilter.TickSize) * priceFilter.TickSize;

                _bands.Add(band);
            }

            // apply the significant buy orders to the bands
            foreach (var order in _orders.Where(x => x.Side == OrderSide.Buy))
            {
                var band = new Band
                {
                    Quantity = order.OriginalQuantity,
                    OpenPrice = order.Price,
                    ClosePrice = order.Price * _options.TargetMultiplier,
                    Status = BandStatus.Ordered
                };

                band.OpenOrderIds.Add(order.OrderId);

                _bands.Add(band);
            }

            // identify bands where the target sell is somehow below the notional filter
            var small = new HashSet<Band>();
            foreach (var band in _bands.Where(x => x.Status == BandStatus.Open && x.Quantity * x.ClosePrice < minNotionalFilter.MinNotional).ToList())
            {
                if (_bands.Remove(band))
                {
                    small.Add(band);
                }
            }

            // group all the small bands into a new band
            if (small.Count > 0)
            {
                var leftovers = new Band
                {
                    Quantity = small.Sum(x => x.Quantity),
                    OpenPrice = small.Sum(x => x.OpenPrice * x.Quantity) / small.Sum(x => x.Quantity),
                    Status = BandStatus.Open
                };
                leftovers.ClosePrice = leftovers.OpenPrice * _options.TargetMultiplier;
                leftovers.OpenOrderIds.UnionWith(small.SelectMany(x => x.OpenOrderIds));

                // see if the leftovers are sellable
                if (leftovers.Quantity * leftovers.ClosePrice >= minNotionalFilter.MinNotional)
                {
                    _bands.Add(leftovers);
                }
                else
                {
                    _logger.LogWarning(
                        "{Type} {Name} ignoring under notional leftovers of {Quantity} {Asset} opening at {OpenPrice} {Quote} and closing at {ClosePrice} {Quote}",
                        Type, _name, leftovers.Quantity, _options.Asset, leftovers.OpenPrice, _options.Quote, leftovers.ClosePrice, _options.Quote);
                }
            }

            // identify the applicable sell orders for each band
            var used = new HashSet<OrderQueryResult>();
            foreach (var band in _bands)
            {
                var order = _orders.Except(used).FirstOrDefault(x => x.Side == OrderSide.Sell && x.OriginalQuantity == band.Quantity && x.Price == band.ClosePrice);
                if (order is not null)
                {
                    band.CloseOrderId = order.OrderId;
                    used.Add(order);
                }
            }

            _logger.LogInformation(
                "{Type} {Name} is managing {Count} bands",
                Type, _name, _bands.Count, _bands);

            // always let the algo continue
            return false;
        }

        public IEnumerable<AccountTrade> GetTrades()
        {
            return _trades.ToImmutableList();
        }

        #region Classes

        private class SignificantTracker
        {
            public decimal RemainingQuantity { get; set; }
        }

        private enum BandStatus
        {
            Ordered,
            Open
        }

        private class Band : IComparable<Band>
        {
            public Guid Id { get; } = Guid.NewGuid();
            public HashSet<long> OpenOrderIds { get; } = new HashSet<long>();
            public decimal Quantity { get; set; }
            public decimal OpenPrice { get; set; }
            public BandStatus Status { get; set; }

            public long CloseOrderId { get; set; }

            public decimal ClosePrice { get; set; }

            public int CompareTo(Band? other)
            {
                _ = other ?? throw new ArgumentNullException(nameof(other));

                var byOpenPrice = OpenPrice.CompareTo(other.OpenPrice);
                if (byOpenPrice is not 0) return byOpenPrice;

                var byId = Id.CompareTo(other.Id);
                if (byId is not 0) return byId;

                return 0;
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