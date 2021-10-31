using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.ValueAveraging
{
    internal class ValueAveragingAlgo : SymbolAlgo
    {
        private readonly ValueAveragingAlgoOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public ValueAveragingAlgo(IOptions<ValueAveragingAlgoOptions> options, ILogger<ValueAveragingAlgo> logger, ISystemClock clock)
        {
            _options = options.Value;
            _logger = logger;
            _clock = clock;
        }

        private static string TypeName => nameof(ValueAveragingAlgo);

        private decimal _smaA;
        private decimal _smaB;
        private decimal _smaC;
        private decimal _rsiA;
        private decimal _rsiB;
        private decimal _rsiC;

        public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            // get the lastest klines
            var maxPeriods = GetMaxPeriods();
            var end = _clock.UtcNow;
            var start = end.Subtract(_options.KlineInterval, maxPeriods);
            var klines = await Context.GetKlinesAsync(Context.Symbol.Name, _options.KlineInterval, start, end, cancellationToken);

            // calculate the current moving averages
            _smaA = klines.LastSimpleMovingAverage(x => x.ClosePrice, _options.SmaPeriodsA);
            _smaB = klines.LastSimpleMovingAverage(x => x.ClosePrice, _options.SmaPeriodsB);
            _smaC = klines.LastSimpleMovingAverage(x => x.ClosePrice, _options.SmaPeriodsC);

            // calculate the rsi values
            _rsiA = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, _options.RsiPeriodsA);
            _rsiB = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, _options.RsiPeriodsB);
            _rsiC = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, _options.RsiPeriodsC);

            // evaluate signals and return results for them
            return Many(

                // set a tracking buy if we hit a buy signal
                TrySignalBuyOrder()
                    ? SetTrackingBuy()
                    : ClearOpenOrders(Context.Symbol, OrderSide.Buy),

                // place an averaging sell if we hit a sell signal
                TrySignalSellOrder()
                    ? SignificantAveragingSell(Context.Ticker, Context.Significant.Orders, _options.MinSellProfitRate, _options.RedeemSavings)
                    : ClearOpenOrders(Context.Symbol, OrderSide.Sell));
        }

        private int GetMaxPeriods()
        {
            return MathSpan.Max(stackalloc int[] { _options.SmaPeriodsA, _options.SmaPeriodsB, _options.SmaPeriodsC, _options.RsiPeriodsA, _options.RsiPeriodsB, _options.RsiPeriodsC });
        }

        [SuppressMessage("Blocker Code Smell", "S2178:Short-circuit logic should be used in boolean contexts", Justification = "Indicators")]
        private bool TrySignalBuyOrder()
        {
            return
                IsOpeningEnabled() &
                IsAveragingEnabled() &
                IsTickerLowerThanSafePrice() &
                IsSmaTrendingDown() &
                IsTickerBelowSmas() &
                IsRsiTrendingDown() &
                IsRsiOversold();
        }

        private IAlgoCommand SetTrackingBuy()
        {
            _logger.LogInformation(
                    "{Type} {Symbol} will signal a buy order for the current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                    TypeName, Context.Symbol.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);

            return TrackingBuy(Context.Symbol, _options.BuyOrderSafetyRatio, _options.BuyQuoteBalanceFraction, _options.MaxNotional, _options.RedeemSavings);
        }

        private bool IsRsiTrendingDown()
        {
            var indicator = _rsiA < _rsiB && _rsiB < _rsiC;

            _logger.LogInformation(
                "{Type} {Symbol} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) trending down = {Indicator}",
                TypeName, Context.Symbol.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

            return indicator;
        }

        private bool IsRsiOversold()
        {
            var indicator = _rsiA < _options.RsiOversoldA && _rsiB < _options.RsiOversoldB && _rsiC < _options.RsiOversoldC;

            _logger.LogInformation(
                "{Type} {Symbol} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) oversold = {Indicator}",
                TypeName, Context.Symbol.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

            return indicator;
        }

        private bool IsSmaTrendingDown()
        {
            var indicator = _smaA < _smaB && _smaB < _smaC;

            _logger.LogInformation(
                "{Type} {Symbol} reports (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) trending down = {Indicator}",
                TypeName, Context.Symbol.Name, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsTickerBelowSmas()
        {
            var indicator = Context.Ticker.ClosePrice < _smaA && Context.Ticker.ClosePrice < _smaB && Context.Ticker.ClosePrice < _smaC;

            _logger.LogInformation(
                "{Type} {Symbol} reports Ticker {Ticker:F8} below (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) = {Indicator}",
                TypeName, Context.Symbol.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsSmaTrendingUp()
        {
            var indicator = _smaA > _smaB && _smaB > _smaC;

            _logger.LogInformation(
                "{Type} {Symbol} reports (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) trending up = {Indicator}",
                TypeName, Context.Symbol.Name, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsTickerAboveSmas()
        {
            var indicator = Context.Ticker.ClosePrice > _smaA && Context.Ticker.ClosePrice > _smaB && Context.Ticker.ClosePrice > _smaC;

            _logger.LogInformation(
                "{Type} {Symbol} reports Ticker {Ticker:F8} above (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) = {Indicator}",
                TypeName, Context.Symbol.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsRsiOverbought()
        {
            var indicator = _rsiA > _options.RsiOverboughtA && _rsiB > _options.RsiOverboughtB && _rsiC > _options.RsiOverboughtC;

            _logger.LogInformation(
                "{Type} {Symbol} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) overbought = {Indicator}",
                TypeName, Context.Symbol.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

            return indicator;
        }

        private bool IsRsiTrendingUp()
        {
            var indicator = _rsiA > _rsiB && _rsiB > _rsiC;

            _logger.LogInformation(
                "{Type} {Symbol} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) trending up = {Indicator}",
                TypeName, Context.Symbol.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

            return indicator;
        }

        private bool IsTickerLowerThanSafePrice()
        {
            // skip this rule if there are no orders to evaluate
            if (Context.Significant.Orders.Count == 0)
            {
                return true;
            }

            // pin the last significant order
            var last = Context.Significant.Orders.Max!;

            // skip this rule if the significant total of the last order is under the minimum notional (leftovers)
            if (last.ExecutedQuantity * last.Price < Context.Symbol.Filters.MinNotional.MinNotional)
            {
                return true;
            }

            // skip this rule if the significant quantity of the last order is under the minimum lot size (leftovers)
            if (last.ExecutedQuantity < Context.Symbol.Filters.LotSize.MinQuantity)
            {
                return true;
            }

            // break on price not low enough from last significant buy
            var minPrice = last.Price;
            var lowPrice = minPrice * _options.PullbackRatio;
            if (Context.Ticker.ClosePrice > lowPrice)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected ticker of {Ticker:F8} is above the low price of {LowPrice:F8} calculated as {PullBackRatio:F8} of the min significant buy price of {MinPrice:F8} and will not signal a buy order",
                    TypeName, Context.Symbol.Name, Context.Ticker.ClosePrice, lowPrice, _options.PullbackRatio, minPrice);

                return false;
            }

            // otherwise skip this rule by default
            return true;
        }

        private bool IsAveragingEnabled()
        {
            // break on disabled averaging
            if (Context.Significant.Orders.Count > 0 && !_options.IsAveragingEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} has averaging disabled and will not signal a buy order",
                    TypeName, Context.Symbol.Name);

                return false;
            }

            return true;
        }

        private bool IsOpeningEnabled()
        {
            // break on disabled opening
            if (Context.Significant.Orders.Count == 0 && !_options.IsOpeningEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} has opening disabled and will not signal a buy order",
                    TypeName, Context.Symbol.Name);

                return false;
            }

            return true;
        }

        [SuppressMessage("Blocker Code Smell", "S2178:Short-circuit logic should be used in boolean contexts", Justification = "Indicators")]
        private bool TrySignalSellOrder()
        {
            var signal =
                IsSmaTrendingUp() &
                IsTickerAboveSmas() &
                IsRsiTrendingUp() &
                IsRsiOverbought();

            if (signal)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} signalling sell for current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                    TypeName, Context.Symbol.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);
            }

            return signal;
        }
    }
}