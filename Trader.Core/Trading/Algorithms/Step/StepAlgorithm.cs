using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;

namespace Trader.Core.Trading.Algorithms.Step
{
    internal class StepAlgorithm : IStepAlgorithm
    {
        private readonly string _name;

        private readonly ILogger _logger;
        private readonly StepAlgorithmOptions _options;

        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;
        private readonly ISafeTimer _timer;

        public StepAlgorithm(string name, ILogger<StepAlgorithm> logger, IOptionsSnapshot<StepAlgorithmOptions> options, ISafeTimerFactory factory, ISystemClock clock, ITradingService trader)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));

            _timer = factory.Create(TickAsync, TimeSpan.Zero, _options.Tick);
        }

        private static string Type => nameof(StepAlgorithm);

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{Type} {Name} starting...", Type, _name);

            await SyncExchangeParametersAsync(cancellationToken);

            await _timer.StartAsync(cancellationToken);

            _logger.LogInformation("{Type} {Name} started", Type, _name);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _timer.StopAsync(cancellationToken);
            _logger.LogInformation("{Type} {Name} stopped", Type, _name);

            return Task.CompletedTask;
        }

        private readonly CancellationTokenSource _cancellation = new();
        private readonly Balances _balances = new();

        /// <summary>
        /// Keeps useful exchange parameters.
        /// </summary>
        private readonly ExchangeParameters _parameters = new();

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

        /// <summary>
        /// Tracks the latest asset price;
        /// </summary>
        private SymbolPriceTicker? _ticker;

        private async Task SyncExchangeParametersAsync(CancellationToken cancellationToken = default)
        {
            var exchange = await _trader.GetExchangeInfoAsync(cancellationToken);

            _parameters.Symbol = exchange.Symbols.Single(x => x.Name == _options.Symbol);
            _parameters.PriceFilter = _parameters.Symbol.Filters.OfType<PriceSymbolFilter>().Single();
            _parameters.LotSizeFilter = _parameters.Symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            _parameters.MinNotionalFilter = _parameters.Symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();
        }

        private async Task SyncAccountInfoAsync()
        {
            _logger.LogInformation("{Type} {Name} querying account information...", Type, _name);

            var account = await _trader.GetAccountInfoAsync(new GetAccountInfo(null, _clock.UtcNow), _cancellation.Token);

            var gotAsset = false;
            var gotQuote = false;

            foreach (var balance in account.Balances)
            {
                if (balance.Asset == _options.Asset)
                {
                    _balances.Asset.Free = balance.Free;
                    _balances.Asset.Locked = balance.Locked;
                    gotAsset = true;

                    _logger.LogInformation(
                        "{Type} {Name} reports balance for base asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                        Type, _name, _options.Asset, balance.Free, balance.Locked, balance.Free + balance.Locked);
                }
                else if (balance.Asset == _options.Quote)
                {
                    _balances.Quote.Free = balance.Free;
                    _balances.Quote.Locked = balance.Locked;
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

        private async Task SyncAssetPriceAsync()
        {
            _ticker = await _trader.GetSymbolPriceTickerAsync(_options.Symbol, _cancellation.Token);

            _logger.LogInformation(
                "{Type} {Name} reports latest asset price is {Price} {QuoteAsset}",
                Type, _name, _ticker.Price, _options.Quote);
        }

        private async Task TickAsync(IDisposable timer)
        {
            // sync trading information
            // todo: these only need to resync after an algo operation
            await SyncAccountInfoAsync();
            await SyncAccountOpenOrdersAsync();
            await SyncAccountTradesAsync();

            // always update the latest price
            await SyncAssetPriceAsync();

            // run the magic code
            await RunAlgorithmAsync();
        }

        private async Task RunAlgorithmAsync()
        {
            if (TryIdentifySignificantTrades()) return;
            if (TrySyncTradingBands()) return;
            if (await TrySetStartingTradeAsync()) return;
            if (await TryCancelRogueSellOrdersAsync()) return;
            if (await TrySetBandSellOrdersAsync()) return;
            if (await TryCreateLowerBandOrderAsync()) return;
            if (await TryCloseOutOfRangeBandsAsync()) return;
        }

        private async Task<bool> TryCloseOutOfRangeBandsAsync()
        {
            var threshold = _ticker.Price / _options.TargetMultiplier / _options.TargetMultiplier;

            foreach (var band in _bands.Where(x => x.Status == BandStatus.Ordered && x.OpenPrice < threshold))
            {
                var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, band.OpenOrderId, null, null, null, _clock.UtcNow));

                _logger.LogInformation(
                    "{Type} {Name} closed out-of-range {OrderSide} {OrderType} for {Quantity} {Asset} at {Price} {Quote}",
                    Type, _name, result.Side, result.Type, result.OriginalQuantity, _options.Asset, result.Price, _options.Quote);

                return true;
            }

            return false;
        }

        private async Task<bool> TryCreateLowerBandOrderAsync()
        {
            // validate requirements
            if (_parameters.PriceFilter is null) throw new AlgorithmException($"{nameof(_parameters.PriceFilter)} is not available");
            if (_ticker is null) throw new AlgorithmException($"{nameof(_ticker)} is not available");
            if (_parameters.Symbol is null) throw new AlgorithmException($"{nameof(_parameters.Symbol)} is not available");
            if (_parameters.LotSizeFilter is null) throw new AlgorithmException($"{nameof(_parameters.LotSizeFilter)} is not available");

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
            if (_ticker.Price >= lowBand.OpenPrice)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports price {Price} {Quote} is within the current band of {OpenPrice} {Quote} to {ClosePrice} {Quote} and will skip new band creation",
                    Type, _name, _ticker.Price, _options.Quote, lowBand.OpenPrice, _options.Quote, lowBand.ClosePrice, _options.Quote);

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
            while (lowerPrice > _ticker.Price)
            {
                lowerPrice /= _options.TargetMultiplier;
            }

            // protect some weird stuff
            if (lowerPrice <= 0)
            {
                throw new AlgorithmException($"Somehow we got to a lower price of {lowerPrice}!");
            }

            // under adjust the buy price to the tick size
            lowerPrice = Math.Floor(lowerPrice / _parameters.PriceFilter.TickSize) * _parameters.PriceFilter.TickSize;

            // calculate the amount to pay with
            var total = Math.Round(Math.Max(_balances.Quote.Free * _options.TargetQuoteBalanceFractionPerBand, _options.MinQuoteAssetQuantityPerOrder), _parameters.Symbol.QuoteAssetPrecision);

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
            quantity = Math.Floor(quantity / _parameters.LotSizeFilter.StepSize) * _parameters.LotSizeFilter.StepSize;

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

        private async Task<bool> TrySetStartingTradeAsync()
        {
            // validate requirements
            if (_ticker is null) throw new AlgorithmException($"{nameof(_ticker)} has not been populated");
            if (_parameters.Symbol is null) throw new AlgorithmException($"{nameof(_parameters.Symbol)} has not been populated");
            if (_parameters.PriceFilter is null) throw new AlgorithmException($"{nameof(_parameters.PriceFilter)} has not been populated");
            if (_parameters.LotSizeFilter is null) throw new AlgorithmException($"{nameof(_parameters.LotSizeFilter)} has not been populated");

            // only manage the opening if there are no bands or only a single order band to move around
            if (_bands.Count == 0 || (_bands.Count == 1 && _bands.Single().Status == BandStatus.Ordered))
            {
                // cancel any rogue open sell orders - this should not be possible given balance is zero at this point
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
                var lowBuyPrice = _ticker.Price / _options.TargetMultiplier;

                // under adjust the buy price to the tick size
                lowBuyPrice = Math.Floor(lowBuyPrice / _parameters.PriceFilter.TickSize) * _parameters.PriceFilter.TickSize;

                _logger.LogInformation(
                    "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                    Type, _name, lowBuyPrice, _options.Quote, _ticker.Price, _options.Quote);

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
                    var total = Math.Round(Math.Max(_balances.Quote.Free * _options.TargetQuoteBalanceFractionPerBand, _options.MinQuoteAssetQuantityPerOrder), _parameters.Symbol.QuoteAssetPrecision);

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
                    quantity = Math.Floor(quantity / _parameters.LotSizeFilter.StepSize) * _parameters.LotSizeFilter.StepSize;

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

        private bool TryIdentifySignificantTrades()
        {
            _significant.Clear();

            var total = _balances.Asset.Total;

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
            if (total is not 0)
            {
                _logger.LogError(
                    "{Type} {Name} could not identify all significant trades that make up the current asset balance of {Total}",
                    Type, _name, _balances.Asset.Total);

                return true;
            }

            _logger.LogInformation(
                "{Type} {Name} identified {Count} significant trades that make up the asset balance of {Total}",
                Type, _name, _significant.Count, _balances.Asset.Total);

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

        private bool TrySyncTradingBands()
        {
            if (_parameters.PriceFilter is null) throw new AlgorithmException($"{nameof(_parameters.PriceFilter)} is required");

            _bands.Clear();

            // apply the significant buy trades to the bands
            foreach (var group in _significant.Where(x => x.IsBuyer).GroupBy(x => x.OrderId))
            {
                var band = new Band
                {
                    OpenOrderId = group.Key,
                    Quantity = group.Sum(x => x.Quantity),
                    OpenPrice = group.Sum(x => x.Price * x.Quantity) / group.Sum(x => x.Quantity),
                    ClosePrice = (group.Sum(x => x.Price * x.Quantity) / group.Sum(x => x.Quantity)) * _options.TargetMultiplier,
                    Status = BandStatus.Open
                };

                // adjust the target price to the tick size
                band.ClosePrice = Math.Floor(band.ClosePrice / _parameters.PriceFilter.TickSize) * _parameters.PriceFilter.TickSize;

                _bands.Add(band);
            }

            // apply the significant buy orders to the bands
            foreach (var order in _orders.Where(x => x.Side == OrderSide.Buy))
            {
                _bands.Add(new Band
                {
                    OpenOrderId = order.OrderId,
                    Quantity = order.OriginalQuantity,
                    OpenPrice = order.Price,
                    ClosePrice = order.Price * _options.TargetMultiplier,
                    Status = BandStatus.Ordered
                });
            }

            // identify bands where the target sell is somehow below the notional filter
            foreach (var band in _bands.Where(x => x.Quantity * x.ClosePrice < _parameters.MinNotionalFilter.MinNotional).ToList())
            {
                _bands.Remove(band);

                _logger.LogWarning(
                    "{Type} {Name} ignoring under notional band of {Quantity} {Asset} opening at {OpenPrice} {Quote} and closing at {ClosePrice} {Quote}",
                    Type, _name, band.Quantity, _options.Asset, band.OpenPrice, _options.Quote, band.ClosePrice, _options.Quote);
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
                "{Type} {Name} added {Count} bands as {@Bands}",
                Type, _name, _bands.Count, _bands);

            // always let the algo continue
            return false;
        }

        #region Classes

        private class ExchangeParameters
        {
            public Symbol? Symbol { get; set; }
            public PriceSymbolFilter? PriceFilter { get; set; }
            public LotSizeSymbolFilter? LotSizeFilter { get; set; }
            public MinNotionalSymbolFilter? MinNotionalFilter { get; set; }
        }

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
            public long OpenOrderId { get; set; }
            public decimal Quantity { get; set; }
            public decimal OpenPrice { get; set; }
            public BandStatus Status { get; set; }

            public long CloseOrderId { get; set; }

            public decimal ClosePrice { get; set; }

            public int CompareTo(Band? other)
            {
                _ = other ?? throw new ArgumentNullException(nameof(other));

                return
                    OpenPrice < other.OpenPrice ? -1 :
                    OpenPrice > other.OpenPrice ? 1 :
                    OpenOrderId < other.OpenOrderId ? -1 :
                    OpenOrderId > other.OpenOrderId ? 1 :
                    0;
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