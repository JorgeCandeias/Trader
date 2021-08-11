using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;
using Trader.Trading.Indicators;
using static System.String;

namespace Trader.Trading.Algorithms.Change
{
    internal class ChangeAlgorithm : ITradingAlgorithm
    {
        private readonly string _name;
        private readonly ILogger _logger;
        private readonly ChangeAlgorithmOptions _options;
        private readonly ITradingRepository _repository;
        private readonly ISignificantOrderResolver _significantOrderResolver;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;

        public ChangeAlgorithm(string name, ILogger<ChangeAlgorithm> logger, IOptionsSnapshot<ChangeAlgorithmOptions> options, ITradingRepository repository, ISignificantOrderResolver significantOrderResolver, ISystemClock clock, ITradingService trader)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _significantOrderResolver = significantOrderResolver ?? throw new ArgumentNullException(nameof(significantOrderResolver));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private Symbol? _symbol;
        private PriceSymbolFilter? _priceFilter;
        private PercentPriceSymbolFilter? _percentFilter;
        private LotSizeSymbolFilter? _lotSizeFilter;
        private MinNotionalSymbolFilter? _minNotionalFilter;

        private Balance? _assetBalance;
        private Balance? _quoteBalance;
        private SignificantResult? _significant;

        public string Symbol => _symbol?.Name ?? Empty;

        public Task InitializeAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            if (exchangeInfo is null) throw new ArgumentNullException(nameof(exchangeInfo));

            _symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            _priceFilter = _symbol.Filters.OfType<PriceSymbolFilter>().Single();
            _percentFilter = _symbol.Filters.OfType<PercentPriceSymbolFilter>().Single();
            _lotSizeFilter = _symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            _minNotionalFilter = _symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            return Task.CompletedTask;
        }

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_significant?.Profit ?? Profit.Zero(_symbol?.QuoteAsset ?? Empty));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.FromProfit(_significant?.Profit ?? Profit.Zero(_symbol?.QuoteAsset ?? Empty)));
        }

        public async Task GoAsync(CancellationToken cancellationToken = default)
        {
            await ApplyAccountInfoAsync(cancellationToken).ConfigureAwait(false);
            await ResolveSignificantOrdersAsync(cancellationToken).ConfigureAwait(false);

            await TryBuyAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task TryBuyAsync(CancellationToken cancellationToken)
        {
            if (_symbol is null) throw new AlgorithmNotInitializedException();
            if (_priceFilter is null) throw new AlgorithmNotInitializedException();
            if (_percentFilter is null) throw new AlgorithmNotInitializedException();
            if (_lotSizeFilter is null) throw new AlgorithmNotInitializedException();
            if (_minNotionalFilter is null) throw new AlgorithmNotInitializedException();
            if (_quoteBalance is null) throw new AlgorithmNotInitializedException();
            if (_significant is null) throw new AlgorithmNotInitializedException();

            // load all recent price history
            var now = _clock.UtcNow;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0, now.Kind);

            var dataStart = now.AddDays(-2);
            var klines = await _repository
                .GetKlinesAsync(_symbol.Name, KlineInterval.Minutes1, dataStart, now, cancellationToken)
                .ConfigureAwait(false);

            // index the klines by open time to support the algo
            var index = klines.ToDictionary(x => x.OpenTime);

            var rsi = klines.Select(x => x.ClosePrice).RelativeStrengthIndex(14).Select(x => Math.Round(x, 2)).TakeLast(14).ToList();
            _logger.LogInformation(
                "{Type} {Name} reports RSI ({Rsi})",
                nameof(ChangeAlgorithm), _name, rsi);

            /*
            // calculate all 24h change metrics
            var changes = new Dictionary<DateTime, decimal>();
            var algoStart = now.AddDays(-1);
            for (var time = algoStart; time <= now; time = time.AddMinutes(1))
            {
                if (index.TryGetValue(time, out var last))
                {
                    var pairTime = time.AddDays(-1);
                    if (index.TryGetValue(pairTime, out var first))
                    {
                        changes[time] = (last.ClosePrice - first.ClosePrice).SafeDivideBy(first.ClosePrice);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "{Type} {Name} detected missing data point {OpenTime} between {StartOpenTime} and {EndOpenTime}",
                            nameof(ChangeAlgorithm), _name, pairTime, dataStart, now);

                        return;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "{Type} {Name} detected missing data point {OpenTime} between {StartOpenTime} and {EndOpenTime}",
                        nameof(ChangeAlgorithm), _name, time, dataStart, now);

                    return;
                }
            }

            // detect a buy signal

            // 1 - check if the rolling 24h change is between the buy thresholds
            var lastChange = changes[now];
            if (lastChange >= _options.BuySignalLowThreshold && lastChange <= _options.BuySignalHighThreshold)
            {
                _logger.LogInformation(
                    "{Type} {Name} detected price change of {Change:P2} is within the buy threshold of {Low:P2} and {High:P2}",
                    nameof(ChangeAlgorithm), _name, lastChange, _options.BuySignalLowThreshold, _options.BuySignalHighThreshold);
            }
            else
            {
                _logger.LogInformation(
                    "{Type} {Name} detected price change of {Change:P2} is outside the buy threshold of {Low:P2} and {High:P2}",
                    nameof(ChangeAlgorithm), _name, lastChange, _options.BuySignalLowThreshold, _options.BuySignalHighThreshold);

                return;
            }

            // 2 - ensure the prior rolling changes over 24h are lower than the last rolling change
            for (var time = algoStart; time < now; time = time.AddMinutes(1))
            {
                var change = changes[time];
                if (change > lastChange)
                {
                    _logger.LogInformation(
                        "{Type} {Name} detected past price change {PastChange:P2} at {PastOpenTime} is above the last change {LastChange:P2} and will not signal buy",
                        nameof(ChangeAlgorithm), _name, change, time, lastChange);

                    return;
                }
            }

            // 3 - ensure no other significant orders exist in the algo window
            var existingOrder = _significant.Orders.FirstOrDefault(x => x.Time >= algoStart && x.Time <= now);
            if (existingOrder is not null)
            {
                _logger.LogInformation(
                    "{Type} {Name} detected significant order of {Quantity} {Asset} for {Price} {Quote} at {Time} within the algo window of {StartOpenTime} and {EndOpenTime} and will not signal buy",
                    nameof(ChangeAlgorithm), _name, existingOrder.OriginalQuantity, _symbol.BaseAsset, existingOrder.Price, _symbol.QuoteAsset, existingOrder.Time, algoStart, now);

                return;
            }

            // 4 - signal the buy at the current close price

            // identify the target low price for the first buy
            var price = index[now].ClosePrice;

            // under adjust the buy price to the tick size
            price = Math.Floor(price / _priceFilter.TickSize) * _priceFilter.TickSize;

            _logger.LogInformation(
                "{Type} {Name} identified buy target price at {Price} {Quote}",
                nameof(ChangeAlgorithm), _name, price, _symbol.QuoteAsset);

            // calculate the amount to pay with
            var total = Math.Round(_quoteBalance.Free * _options.TargetQuoteBalanceFraction, _symbol.QuoteAssetPrecision);

            // raise to the minimum notional if needed
            total = Math.Max(total, _minNotionalFilter.MinNotional);

            // ensure there is enough quote asset for it
            if (total > _quoteBalance.Free)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                    nameof(ChangeAlgorithm), _name, total, _symbol.QuoteAsset, _quoteBalance.Free, _symbol.QuoteAsset);

                return;
            }

            // calculate the appropriate quantity to buy
            var quantity = total / price;

            // round it up to the lot step size
            quantity = Math.Ceiling(quantity / _lotSizeFilter.StepSize) * _lotSizeFilter.StepSize;

            _logger.LogInformation(
                "{Type} {Name} placing {OrderSide} {OrderType} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                nameof(ChangeAlgorithm), _name, OrderSide.Buy, OrderType.Limit, _options.Symbol, quantity, _symbol.BaseAsset, price, _symbol.QuoteAsset, quantity * price, _symbol.QuoteAsset);

            */

            /*
            // we have enough data to create the order now
            var result = await _trader
                .CreateOrderAsync(new Order(
                    _options.Symbol,
                    OrderSide.Buy,
                    OrderType.Limit,
                    TimeInForce.GoodTillCanceled,
                    quantity,
                    null,
                    price,
                    $"{_options.Symbol}{price:N8}".Replace(".", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal),
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
                nameof(ChangeAlgorithm), _name, result.Side, result.Type, result.Symbol, result.OriginalQuantity, _symbol.BaseAsset, result.Price, _symbol.QuoteAsset, result.OriginalQuantity * result.Price, _symbol.QuoteAsset);
            */
        }

        private async Task ApplyAccountInfoAsync(CancellationToken cancellationToken)
        {
            if (_symbol is null) throw new AlgorithmNotInitializedException();

            _assetBalance = await _repository
                .GetBalanceAsync(_symbol.BaseAsset, cancellationToken)
                .ConfigureAwait(false) ??
                throw new AlgorithmException($"Could not get balance for base asset {_symbol.BaseAsset}");

            _logger.LogInformation(
                "{Type} {Name} reports balance for base asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                nameof(ChangeAlgorithm), _name, _symbol.BaseAsset, _assetBalance.Free, _assetBalance.Locked, _assetBalance.Total);

            _quoteBalance = await _repository
                .GetBalanceAsync(_symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false) ??
                throw new AlgorithmException($"Could not get balance for quote asset {_symbol.QuoteAsset}");

            _logger.LogInformation(
                "{Type} {Name} reports balance for quote asset {Asset} is (Free = {Free}, Locked = {Locked}, Total = {Total})",
                nameof(ChangeAlgorithm), _name, _symbol.QuoteAsset, _quoteBalance.Free, _quoteBalance.Locked, _quoteBalance.Total);
        }

        private async Task ResolveSignificantOrdersAsync(CancellationToken cancellationToken)
        {
            if (_symbol is null) throw new AlgorithmNotInitializedException();

            _significant = await _significantOrderResolver
                .ResolveAsync(_symbol, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}