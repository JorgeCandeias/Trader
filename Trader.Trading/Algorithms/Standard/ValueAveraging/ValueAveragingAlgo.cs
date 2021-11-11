using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.ValueAveraging
{
    internal partial class ValueAveragingAlgo : SymbolAlgo
    {
        private readonly ValueAveragingAlgoOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public ValueAveragingAlgo(IOptionsSnapshot<ValueAveragingAlgoOptions> options, ILogger<ValueAveragingAlgo> logger, ISystemClock clock)
        {
            _options = options.Get(Context.Name);
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

        [SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "N/A")]
        protected override async ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            // get the lastest klines
            var maxPeriods = GetMaxPeriods();
            var end = _clock.UtcNow;
            var start = end.Subtract(_options.KlineInterval, maxPeriods);
            var klines = await Context.GetKlinesAsync(Context.Symbol.Name, _options.KlineInterval, start, end, cancellationToken);

            // calculate the current moving averages
            _smaA = klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsA);
            _smaB = klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsB);
            _smaC = klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsC);

            // calculate the rsi values
            _rsiA = klines.LastRsi(x => x.ClosePrice, _options.RsiPeriodsA);
            _rsiB = klines.LastRsi(x => x.ClosePrice, _options.RsiPeriodsB);
            _rsiC = klines.LastRsi(x => x.ClosePrice, _options.RsiPeriodsC);

            // evaluate signals and return results for them
            return Many(

                // set a tracking buy if we hit a buy signal
                TrySignalBuyOrder()
                    ? SetTrackingBuy()
                    : ClearOpenOrders(Context.Symbol, OrderSide.Buy),

                // place an averaging sell if we hit a sell signal
                TrySignalSellOrder()
                    ? _options.ClosingEnabled
                        ? AveragingSell(Context.PositionDetails.Orders, _options.MinSellProfitRate, _options.RedeemSavings, _options.RedeemSwapPool)
                        : SignificantAveragingSell(Context.Ticker, Context.PositionDetails.Orders, _options.MinSellProfitRate, _options.RedeemSavings, _options.RedeemSwapPool)
                    : ClearOpenOrders(Context.Symbol, OrderSide.Sell));
        }

        private int GetMaxPeriods()
        {
            return MathSpan.Max(stackalloc int[] { _options.SmaPeriodsA, _options.SmaPeriodsB, _options.SmaPeriodsC, _options.RsiPeriodsA, _options.RsiPeriodsB, _options.RsiPeriodsC });
        }

        private bool TrySignalBuyOrder()
        {
            return
                IsBuyingEnabled() &&
                IsCooled() &&
                IsBelowPullbackPrice() &&
                IsSmaTrendingDown() &&
                IsTickerBelowSmas() &&
                IsRsiTrendingDown() &&
                IsRsiOversold();
        }

        private IAlgoCommand SetTrackingBuy()
        {
            LogWillSignalBuyOrderForCurrentState(TypeName, Context.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);

            return TrackingBuy(Context.Symbol, _options.BuyOrderSafetyRatio, _options.BuyQuoteBalanceFraction, _options.MaxNotional, _options.RedeemSavings, _options.RedeemSwapPool);
        }

        private bool IsBelowPullbackPrice()
        {
            // skip this rule if no pullback is defined
            if (!_options.PullbackRatio.HasValue)
            {
                return true;
            }

            // skip this rule if there are no positions to compare to
            if (Context.PositionDetails.Orders.Count == 0)
            {
                return true;
            }

            // skip this rule if the remaining positions are under the minimum notional (leftovers)
            if (Context.PositionDetails.Orders.Sum(x => x.Price * x.ExecutedQuantity) < Context.Symbol.Filters.MinNotional.MinNotional)
            {
                return true;
            }

            var price = Context.PositionDetails.Orders.Max!.Price * _options.PullbackRatio.Value;
            var indicator = Context.Ticker.ClosePrice < price;

            LogTickerBelowPullback(TypeName, Context.Name, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset, price, indicator);

            return indicator;
        }

        private bool IsCooled()
        {
            // skip this rule if there are no positions
            if (Context.PositionDetails.Orders.Count == 0)
            {
                return true;
            }

            var indicator = Context.PositionDetails.Orders.Max!.Time.Add(_options.CooldownPeriod) < _clock.UtcNow;

            LogCooldownPeriod(TypeName, Context.Name, _options.CooldownPeriod, Context.PositionDetails.Orders.Max.Time, indicator);

            return indicator;
        }

        private bool IsRsiTrendingDown()
        {
            var indicator = _rsiA < _rsiB && _rsiB < _rsiC;

            LogRsi(TypeName, Context.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

            return indicator;
        }

        private bool IsRsiOversold()
        {
            var indicator = _rsiA < _options.RsiOversoldA && _rsiB < _options.RsiOversoldB && _rsiC < _options.RsiOversoldC;

            LogRsiOversold(TypeName, Context.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

            return indicator;
        }

        private bool IsSmaTrendingDown()
        {
            var indicator = _smaA < _smaB && _smaB < _smaC;

            LogSmaTrendingDown(TypeName, Context.Name, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsTickerBelowSmas()
        {
            var indicator = Context.Ticker.ClosePrice < _smaA && Context.Ticker.ClosePrice < _smaB && Context.Ticker.ClosePrice < _smaC;

            LogTickerBelowSmas(TypeName, Context.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsSmaTrendingUp()
        {
            var indicator = _smaA > _smaB && _smaB > _smaC;

            LogSmaTrendingUp(TypeName, Context.Name, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsTickerAboveSmas()
        {
            var indicator = Context.Ticker.ClosePrice > _smaA && Context.Ticker.ClosePrice > _smaB && Context.Ticker.ClosePrice > _smaC;

            LogTickerAboveSmas(TypeName, Context.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, indicator);

            return indicator;
        }

        private bool IsRsiOverbought()
        {
            var indicator = _rsiA > _options.RsiOverboughtA && _rsiB > _options.RsiOverboughtB && _rsiC > _options.RsiOverboughtC;

            LogRsiOverbought(TypeName, Context.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

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

        private bool IsBuyingEnabled()
        {
            if (!_options.BuyingEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} reports buying is disabled",
                    TypeName, Context.Symbol.Name);

                return false;
            }

            return true;
        }

        private bool IsSellingEnabled()
        {
            if (!_options.SellingEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} reports selling is disabled",
                    TypeName, Context.Symbol.Name);

                return false;
            }

            return true;
        }

        private bool IsClosingEnabled()
        {
            if (_options.ClosingEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} reports closing is enabled.",
                    TypeName, Context.Symbol.Name);

                return true;
            }

            return false;
        }

        private bool IsTickerAboveTargetSellPrice()
        {
            if (Context.PositionDetails.Orders.Count == 0)
            {
                return false;
            }

            var target = Context.PositionDetails.Orders.Max!.Price * _options.TargetSellProfitRate;

            if (Context.Ticker.ClosePrice >= target)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} reports ticker {Ticker:F8} {Asset} is above the target sell price of {Target:F8} {Asset}",
                    TypeName, Context.Name, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset, target, Context.Symbol.QuoteAsset);

                return true;
            }

            return false;
        }

        private bool TrySignalSellOrder()
        {
            if (!IsSellingEnabled())
            {
                return false;
            }

            if (IsClosingEnabled() || IsTickerAboveTargetSellPrice())
            {
                return true;
            }

            var signal =
                IsSmaTrendingUp() &&
                IsTickerAboveSmas() &&
                IsRsiTrendingUp() &&
                IsRsiOverbought();

            if (signal)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} signalling sell for current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                    TypeName, Context.Symbol.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);
            }

            return signal;
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} will signal a buy order for the current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})")]
        private partial void LogWillSignalBuyOrderForCurrentState(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, int rsiPeriodsA, decimal rsiA, int rsiPeriodsB, decimal rsiB, int rsiPeriodsC, decimal rsiC);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports ticker {Ticker:F8} {Asset} below pullback price of {Pullback:F8} {Asset} = {Indicator}")]
        private partial void LogTickerBelowPullback(string type, string name, decimal ticker, string asset, decimal pullback, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports cooldown period of {Cooldown} since last buy at {LastTime} has passed = {Indicator}")]
        private partial void LogCooldownPeriod(string type, string name, TimeSpan cooldown, DateTime lastTime, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) trending down = {Indicator}")]
        private partial void LogRsi(string type, string name, int rsiPeriodsA, decimal rsiA, int rsiPeriodsB, decimal rsiB, int rsiPeriodsC, decimal rsiC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) oversold = {Indicator}")]
        private partial void LogRsiOversold(string type, string name, int rsiPeriodsA, decimal rsiA, int rsiPeriodsB, decimal rsiB, int rsiPeriodsC, decimal rsiC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) trending down = {Indicator}")]
        private partial void LogSmaTrendingDown(string type, string name, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Ticker {Ticker:F8} below (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) = {Indicator}")]
        private partial void LogTickerBelowSmas(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) trending up = {Indicator}")]
        private partial void LogSmaTrendingUp(string type, string name, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Ticker {Ticker:F8} above (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) = {Indicator}")]
        private partial void LogTickerAboveSmas(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) overbought = {Indicator}")]
        private partial void LogRsiOverbought(string type, string name, int rsiPeriodsA, decimal rsiA, int rsiPeriodsB, decimal rsiB, int rsiPeriodsC, decimal rsiC, bool indicator);

        #endregion Logging
    }
}