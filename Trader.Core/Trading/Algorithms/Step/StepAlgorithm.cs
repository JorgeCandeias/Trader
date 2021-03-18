using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
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
        /// Keeps track of the relevant account balances.
        /// </summary>
        private readonly Balances _balances = new();

        /// <summary>
        /// Keeps track of all orders.
        /// </summary>
        private readonly SortedOrderSet _orders = new();

        /// <summary>
        /// Set of orders that compose the current asset balance.
        /// </summary>
        private readonly SortedOrderSet _significant = new();

        /// <summary>
        /// Set of orders that are open now.
        /// </summary>
        private readonly SortedOrderSet _transient = new();

        /// <summary>
        /// Keeps track of all trades.
        /// </summary>
        private readonly SortedTradeSet _trades = new();

        /// <summary>
        /// Keeps an index of trade groups by order id.
        /// </summary>
        private readonly Dictionary<long, SortedTradeSet> _tradesByOrderId = new();

        private readonly SortedSet<Band> _bands = new();

        private void SyncAccountInfo(AccountInfo accountInfo)
        {
            _logger.LogInformation("{Type} {Name} querying account information...", Type, _name);

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

        private async Task SyncAccountOrdersAsync(CancellationToken cancellationToken)
        {
            // pull all new orders page by page
            var newCount = 0;
            ImmutableList<OrderQueryResult> orders;
            do
            {
                orders = await _trader.GetAllOrdersAsync(new GetAllOrders(_options.Symbol, (_orders.Max?.OrderId + 1) ?? 0, null, null, 1000, null, _clock.UtcNow), cancellationToken);

                foreach (var order in orders)
                {
                    // add the new pulled order
                    _orders.Set(order);

                    // if the order is transient then index it
                    if (order.Status.IsTransientStatus())
                    {
                        _transient.Set(order);
                    }

                    ++newCount;
                }
            } while (orders.Count > 0);

            // ensure known transient orders not pulled in the prior step are also updated
            var updatedCount = 0;
            using var copy = ArrayPool<OrderQueryResult>.Shared.SegmentOwnerFrom(_transient);
            foreach (var order in copy.Segment)
            {
                // get the updated order
                var updated = await _trader.GetOrderAsync(new OrderQuery(_options.Symbol, order.OrderId, null, null, _clock.UtcNow), cancellationToken);

                // update the known order
                _orders.Set(updated);

                // update the transient index either way
                if (updated.Status.IsTransientStatus())
                {
                    _transient.Set(updated);
                }
                else
                {
                    _transient.Remove(updated);
                }

                ++updatedCount;
            }

            // pull all new trades
            ImmutableList<AccountTrade> trades;
            do
            {
                trades = await _trader.GetAccountTradesAsync(new GetAccountTrades(_options.Symbol, null, null, (_trades.Max?.Id + 1) ?? 0, 1000, null, _clock.UtcNow), cancellationToken);

                foreach (var trade in trades)
                {
                    // add the trade to the main set
                    _trades.Set(trade);

                    // add the trade to the order index
                    if (!_tradesByOrderId.TryGetValue(trade.OrderId, out var group))
                    {
                        _tradesByOrderId[trade.OrderId] = group = new SortedTradeSet();
                    }
                    group.Set(trade);
                }
            } while (trades.Count > 0);

            // log the activity only if necessary
            _logger.LogInformation(
                "{Type} {Name} pulled {NewCount} new and {UpdatedCount} updated open orders",
                Type, _name, newCount, updatedCount, _orders.Count);
        }

        private async Task<SymbolPriceTicker> SyncAssetPriceAsync()
        {
            var ticker = await _trader.GetSymbolPriceTickerAsync(_options.Symbol, _cancellation.Token);

            _logger.LogInformation(
                "{Type} {Name} reports latest asset price is {Price} {QuoteAsset}",
                Type, _name, ticker.Price, _options.Quote);

            return ticker;
        }

        public async Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            SyncAccountInfo(accountInfo);
            await SyncAccountOrdersAsync(cancellationToken);

            // always update the latest price
            var ticker = await SyncAssetPriceAsync();

            if (TryIdentifySignificantOrders()) return;
            if (TryCreateTradingBands(priceFilter, minNotionalFilter)) return;
            if (await TrySetStartingTradeAsync(symbol, ticker, priceFilter, lotSizeFilter)) return;
            if (await TryCancelRogueSellOrdersAsync()) return;
            if (await TrySetBandSellOrdersAsync()) return;
            if (await TryCreateLowerBandOrderAsync(symbol, ticker, priceFilter, lotSizeFilter)) return;
            if (await TryCloseOutOfRangeBandsAsync(ticker, priceFilter)) return;
        }

        private async Task<bool> TryCloseOutOfRangeBandsAsync(SymbolPriceTicker ticker, PriceSymbolFilter priceFilter)
        {
            // take the lower band
            var band = _bands.Min;
            if (band is null) return false;

            // ensure the lower band is on ordered status
            if (band.Status != BandStatus.Ordered) return false;

            // ensure the lower band is covering the current price
            if (band.OpenPrice < ticker.Price && band.ClosePrice > ticker.Price) return false;

            // if the above checks fails then close the band
            foreach (var orderId in band.OpenOrderIds)
            {
                var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, orderId, null, null, null, _clock.UtcNow));

                _logger.LogInformation(
                    "{Type} {Name} closed out-of-range {OrderSide} {OrderType} for {Quantity} {Asset} at {Price} {Quote}",
                    Type, _name, result.Side, result.Type, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);
            }

            return true;
        }

        private async Task<bool> TryCreateLowerBandOrderAsync(Symbol symbol, SymbolPriceTicker ticker, PriceSymbolFilter priceFilter, LotSizeSymbolFilter lotSizeFilter)
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
            while (lowerPrice >= ticker.Price && lowerPrice >= lowBand.OpenPrice)
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

                    var result = await _trader.CreateOrderAsync(new Order(_options.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, band.Quantity, null, band.ClosePrice, null, null, null, NewOrderResponseType.Full, null, _clock.UtcNow));

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
        private async Task<bool> TryCancelRogueSellOrdersAsync()
        {
            var fail = false;

            foreach (var order in _orders.Where(x => x.Side == OrderSide.Sell && x.Status.IsTransientStatus()))
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

        private async Task<bool> TrySetStartingTradeAsync(Symbol symbol, SymbolPriceTicker ticker, PriceSymbolFilter priceFilter, LotSizeSymbolFilter lotSizeFilter)
        {
            // only manage the opening if there are no bands or only a single order band to move around
            if (_bands.Count == 0 || (_bands.Count == 1 && _bands.Single().Status == BandStatus.Ordered))
            {
                // identify the target low price for the first buy
                var lowBuyPrice = ticker.Price * (1m - _options.TargetPullbackRatio);

                // under adjust the buy price to the tick size
                lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

                _logger.LogInformation(
                    "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                    Type, _name, lowBuyPrice, _options.Quote, ticker.Price, _options.Quote);

                // cancel the lowest open buy order with a open price lower than the lower band to the current price
                foreach (var order in _orders.Where(x => x.Side == OrderSide.Buy && x.Status.IsTransientStatus()))
                {
                    if (order.Price < lowBuyPrice)
                    {
                        var cancelled = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, order.OrderId, null, null, null, _clock.UtcNow));

                        _logger.LogInformation(
                            "{Type} {Name} cancelled low starting open order with price {Price} for {Quantity} units",
                            Type, _name, cancelled.Price, cancelled.OriginalQuantity);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "{Type} {Name} identified a closer opening order for {Quantity} {Asset} at {Price} {Quote} and will leave as-is",
                            Type, _name, order.OriginalQuantity, _options.Asset, order.Price, _options.Quote);
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
                var quantity = total / lowBuyPrice;

                // round it down to the lot size step
                quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

                var result = await _trader.CreateOrderAsync(new Order(
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
                    Type, _name, result.Side, result.Type, result.Symbol, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote, result.OriginalQuantity * result.Price, _options.Quote);

                // skip the rest of this tick to let the algo resync
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool TryIdentifySignificantOrders()
        {
            // todo: keep track of the last significant order start so we avoid slowing down when the orders grow and grow
            // todo: remove the first step and go straight to lifo processing over the entire order set
            // todo: persist all this stuff into sqlite so each tick can operate over the last data only

            // match significant orders to trades so we can sort significant orders by execution date
            var map = new SortedOrderTradeMapSet();
            foreach (var order in _orders)
            {
                if (order.ExecutedQuantity > 0m)
                {
                    map.Add(new OrderTradeMap(order, _tradesByOrderId.TryGetValue(order.OrderId, out var trades) ? trades.ToImmutableList() : ImmutableList<AccountTrade>.Empty));
                }
            }

            // now prune the significant trades to account interim sales
            using var subjects = ArrayPool<OrderTradeMap>.Shared.SegmentOwnerFrom(map);

            for (var i = 0; i < subjects.Segment.Count; ++i)
            {
                // loop through sales forward
                var sell = subjects.Segment[i];
                if (sell.Order.Side == OrderSide.Sell)
                {
                    // loop through buys in lifo order to find the matching buy
                    for (var j = i - 1; j >= 0; --j)
                    {
                        var buy = subjects.Segment[j];
                        if (buy.Order.Side == OrderSide.Buy)
                        {
                            // remove as much as possible from the buy to satisfy the sell
                            var take = Math.Min(buy.RemainingExecutedQuantity, sell.RemainingExecutedQuantity);
                            buy.RemainingExecutedQuantity -= take;
                            sell.RemainingExecutedQuantity -= take;

                            // if the sale is filled then we can break early
                            if (sell.RemainingExecutedQuantity == 0) break;
                        }
                    }

                    // if the sell was not filled then we're missing some data
                    if (sell.RemainingExecutedQuantity != 0)
                    {
                        // something went very wrong if we got here
                        _logger.LogError(
                            "{Type} {Name} could not fill significant {Side} order {OrderId} with for {Quantity} {Asset} at {Price} {Quote}",
                            Type, _name, sell.Order.Side, sell.Order.OrderId, sell.Order.ExecutedQuantity, _options.Asset, sell.Order.Price, _options.Quote);

                        return true;
                    }
                }
            }

            // keep only buys with some quantity left to sell
            _significant.Clear();
            foreach (var subject in subjects.Segment)
            {
                if (subject.Order.Side == OrderSide.Buy && subject.RemainingExecutedQuantity > 0)
                {
                    _significant.Add(new OrderQueryResult(
                        subject.Order.Symbol,
                        subject.Order.OrderId,
                        subject.Order.OrderListId,
                        subject.Order.ClientOrderId,
                        subject.Order.Price,
                        subject.Order.OriginalQuantity,
                        subject.RemainingExecutedQuantity,
                        subject.Order.CummulativeQuoteQuantity,
                        subject.Order.Status,
                        subject.Order.TimeInForce,
                        subject.Order.Type,
                        subject.Order.Side,
                        subject.Order.StopPrice,
                        subject.Order.IcebergQuantity,
                        subject.Order.Time,
                        subject.Order.UpdateTime,
                        subject.Order.IsWorking,
                        subject.Order.OriginalQuoteOrderQuantity));
                }
            }

            _logger.LogInformation(
                "{Type} {Name} identified {Count} significant trades that make up the asset balance of {Total}",
                Type, _name, _significant.Count, _balances.Asset.Total);

            return false;
        }

        private bool TryCreateTradingBands(PriceSymbolFilter priceFilter, MinNotionalSymbolFilter minNotionalFilter)
        {
            _bands.Clear();

            // apply the significant buy orders to the bands
            foreach (var order in _significant.Where(x => x.Side == OrderSide.Buy))
            {
                if (order.Status.IsTransientStatus())
                {
                    // add transient orders with original quantity
                    _bands.Add(new Band
                    {
                        Quantity = order.OriginalQuantity,
                        OpenPrice = order.Price,
                        OpenOrderIds = { order.OrderId },
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
                        OpenOrderIds = { order.OrderId },
                        Status = BandStatus.Open
                    });
                }
            }

            // apply the non-significant open buy orders to the bands
            foreach (var order in _transient.Where(x => x.Side == OrderSide.Buy && x.ExecutedQuantity == 0m))
            {
                // add transient orders with original quantity
                _bands.Add(new Band
                {
                    Quantity = order.OriginalQuantity,
                    OpenPrice = order.Price,
                    OpenOrderIds = { order.OrderId },
                    Status = BandStatus.Ordered
                });
            }

            // skip if no bands were created
            if (_bands.Count == 0) return false;

            // figure out the constant step size
            var stepSize = _bands.Max!.OpenPrice * _options.TargetPullbackRatio;

            // adjust close prices on the bands
            foreach (var band in _bands)
            {
                band.ClosePrice = band.OpenPrice + stepSize;
                band.ClosePrice = Math.Floor(band.ClosePrice / priceFilter.TickSize) * priceFilter.TickSize;
            }

            // apply open sell orders to the bands
            // todo: optimize this enumeration on all the orders - we will only need a few
            var used = new HashSet<Band>();
            foreach (var order in _orders.Where(x => x.Side == OrderSide.Sell && x.Status.IsTransientStatus()))
            {
                var band = _bands.Except(used).FirstOrDefault(x => x.Status == BandStatus.Open && x.Quantity == order.OriginalQuantity && x.ClosePrice == order.Price);
                if (band is not null)
                {
                    band.CloseOrderId = order.OrderId;
                    used.Add(band);
                }
            }

            // identify bands where the target sell is somehow below the notional filter
            foreach (var band in _bands.Where(x => x.Status == BandStatus.Open && x.Quantity * x.ClosePrice < minNotionalFilter.MinNotional).ToList())
            {
                // todo: group these bands and sell them together
                _logger.LogWarning(
                    "{Type} {Name} ignoring under notional band of {Quantity} {Asset} opening at {OpenPrice} {Quote} and closing at {ClosePrice} {Quote}",
                    Type, _name, band.Quantity, _options.Asset, band.OpenPrice, _options.Quote, band.ClosePrice, _options.Quote);

                _bands.Remove(band);
            }

            _logger.LogInformation(
                "{Type} {Name} is managing {Count} bands",
                Type, _name, _bands.Count, _bands);

            // always let the algo continue
            return false;
        }

        public Task<ImmutableList<AccountTrade>> GetTradesAsync()
        {
            return Task.FromResult(_trades.ToImmutableList());
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

        private class SortedOrderSet : SortedSet<OrderQueryResult>
        {
            public SortedOrderSet() : base(OrderIdComparer.Instance)
            {
            }

            private class OrderIdComparer : IComparer<OrderQueryResult>
            {
                private OrderIdComparer()
                {
                }

                public int Compare(OrderQueryResult? x, OrderQueryResult? y)
                {
                    if (x is null) throw new ArgumentNullException(nameof(x));
                    if (y is null) throw new ArgumentNullException(nameof(y));

                    return x.OrderId.CompareTo(y.OrderId);
                }

                public static OrderIdComparer Instance { get; } = new OrderIdComparer();
            }
        }

        private class SortedTradeSet : SortedSet<AccountTrade>
        {
            public SortedTradeSet() : base(TradeIdComparer.Instance)
            {
            }

            private class TradeIdComparer : IComparer<AccountTrade>
            {
                private TradeIdComparer()
                {
                }

                public int Compare(AccountTrade? x, AccountTrade? y)
                {
                    if (x is null) throw new ArgumentNullException(nameof(x));
                    if (y is null) throw new ArgumentNullException(nameof(y));

                    return x.Id.CompareTo(y.Id);
                }

                public static TradeIdComparer Instance { get; } = new TradeIdComparer();
            }
        }

        /// <summary>
        /// Maps an order to any resulting trades.
        /// </summary>
        private class OrderTradeMap
        {
            public OrderTradeMap(OrderQueryResult order, ImmutableList<AccountTrade> trades)
            {
                Order = order ?? throw new ArgumentNullException(nameof(order));
                Trades = trades ?? throw new ArgumentNullException(nameof(trades));

                MaxTradeTime = Trades.Count > 0 ? Trades.Max(x => x.Time) : null;
                MaxEventTime = MaxTradeTime ?? Order.Time;

                RemainingExecutedQuantity = Order.ExecutedQuantity;
            }

            public OrderQueryResult Order { get; }
            public ImmutableList<AccountTrade> Trades { get; }

            public DateTime? MaxTradeTime { get; }
            public DateTime MaxEventTime { get; }

            public decimal RemainingExecutedQuantity { get; set; }
        }

        private class SortedOrderTradeMapSet : SortedSet<OrderTradeMap>
        {
            public SortedOrderTradeMapSet() : base(OrderTradeMapComparer.Instance)
            {
            }

            private class OrderTradeMapComparer : IComparer<OrderTradeMap>
            {
                private OrderTradeMapComparer()
                {
                }

                public int Compare(OrderTradeMap? x, OrderTradeMap? y)
                {
                    if (x is null) throw new ArgumentNullException(nameof(x));
                    if (y is null) throw new ArgumentNullException(nameof(y));

                    // keep the set sorted by max event time
                    var byEventTime = x.MaxEventTime.CompareTo(y.MaxEventTime);
                    if (byEventTime is not 0) return byEventTime;

                    // resort to order id if needed
                    return x.Order.OrderId.CompareTo(y.Order.OrderId);
                }

                public static OrderTradeMapComparer Instance { get; } = new OrderTradeMapComparer();
            }
        }

        #endregion Classes
    }
}