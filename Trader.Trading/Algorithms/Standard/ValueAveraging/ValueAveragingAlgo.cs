﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Trading.Algorithms.Standard.ValueAveraging
{
    internal sealed partial class ValueAveragingAlgo : Algo
    {
        private readonly ValueAveragingAlgoOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public ValueAveragingAlgo(IOptionsMonitor<ValueAveragingAlgoOptions> options, ILogger<ValueAveragingAlgo> logger, ISystemClock clock)
        {
            _options = options.Get(Context.Name);
            _logger = logger;
            _clock = clock;
        }

        private static string TypeName => nameof(ValueAveragingAlgo);

        private decimal _smaA;
        private decimal _smaB;
        private decimal _smaC;
        private decimal _rsi;

        [SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "N/A")]
        protected override async ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            // calculate the current moving averages
            _smaA = Context.Klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsA);
            _smaB = Context.Klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsB);
            _smaC = Context.Klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsC);

            // calculate the rsi values
            _rsi = Context.Klines.LastRsi(x => x.ClosePrice, _options.RsiPeriods);

            // decide on buying
            var buyCommand = await TrySignalBuyOrder()
                ? CreateBuy()
                : CancelOpenOrders(Context.Symbol, OrderSide.Buy);

            // decide on selling
            var sellCommand = TrySignalSellOrder()
                ? IsClosingEnabled()
                    ? AveragingSell(Context.Symbol, Context.PositionDetails.Orders, _options.MinSellProfitRate, _options.RedeemSavings, _options.RedeemSwapPool, _options.TopUpUnsellablePositionWithBalance)
                    : SignificantAveragingSell(Context.Symbol, Context.Ticker, Context.PositionDetails.Orders, _options.MinSellProfitRate, _options.RedeemSavings, _options.RedeemSwapPool)
                : CancelOpenOrders(Context.Symbol, OrderSide.Sell, 0.01M);

            return Many(buyCommand, sellCommand);
        }

        private async ValueTask<bool> TrySignalBuyOrder()
        {
            var signal =
                IsBuyingEnabled() &&
                (
                    (IsCooled() && IsRsiOversold()) ||
                    (IsAccumulationEnabled() && await IsTickerOnNextStep())
                );

            if (signal)
            {
                LogWillSignalBuyOrderForCurrentState(TypeName, Context.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriods, _rsi);
            }

            return signal;
        }

        private bool TrySignalSellOrder()
        {
            var signal =
                IsSellingEnabled() &&
                IsPositionNonEmpty() &&
                (
                    IsClosingEnabled() ||
                    IsTickerAboveTakeProfitRate() ||
                    IsTickerBelowTrailingStopLoss() ||
                    IsRsiOverbought()
                );

            if (signal)
            {
                LogSignallingSell(TypeName, Context.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriods, _rsi);
            }

            return signal;
        }

        private bool IsPositionNonEmpty() => Context.PositionDetails.Orders.Count > 0;

        private IAlgoCommand CreateBuy()
        {
            var total = Context.GetQuoteBaseAssetBalance(_options.RedeemSavings, _options.RedeemSwapPool) * _options.BuyQuoteBalanceFraction;

            total = total.AdjustTotalUpToMinNotional(Context.Symbol);

            if (_options.MaxNotional.HasValue)
            {
                total = Math.Max(total, _options.MaxNotional.Value);
            }

            var quantity = total / Context.Ticker.ClosePrice;

            return MarketBuy(Context.Symbol, quantity, _options.RedeemSavings, _options.RedeemSwapPool);
        }

        private async ValueTask<bool> IsTickerOnNextStep()
        {
            // only evaluate this rule if there are positions
            if (Context.PositionDetails.Orders.Count == 0)
            {
                return false;
            }

            // only evaluate this rule if the last trade was a buy trade
            // todo: refactor this into the context
            var trades = await Context.GetTradeProvider().GetTradesAsync(Context.Symbol.Name);
            if (!(trades.Count > 0 && trades[trades.Count - 1].IsBuyer))
            {
                return false;
            }

            var order = Context.PositionDetails.Orders.Max!;
            var target = (order.Price * _options.StepRate).AdjustPriceUpToTickSize(Context.Symbol);
            if (Context.Ticker.ClosePrice >= target)
            {
                return true;
            }

            return false;
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

        private bool IsRsiOversold()
        {
            var indicator = _rsi <= _options.RsiOversold;

            LogRsiOversold(TypeName, Context.Name, _options.RsiPeriods, _rsi, indicator);

            return indicator;
        }

        private bool IsRsiOverbought()
        {
            var indicator = _rsi >= _options.RsiOverbought;

            LogRsiOverbought(TypeName, Context.Name, _options.RsiPeriods, _rsi, indicator);

            return indicator;
        }

        private bool IsBuyingEnabled()
        {
            if (!_options.BuyingEnabled)
            {
                LogBuyingDisabled(TypeName, Context.Name);

                return false;
            }

            return true;
        }

        private bool IsAccumulationEnabled()
        {
            if (!_options.AccumulationEnabled)
            {
                LogAccumulationDisabled(TypeName, Context.Name);

                return false;
            }

            return true;
        }

        private bool IsSellingEnabled()
        {
            if (!_options.SellingEnabled)
            {
                LogSellingDisabled(TypeName, Context.Name);

                return false;
            }

            return true;
        }

        private bool IsClosingEnabled()
        {
            if (_options.ClosingEnabled)
            {
                LogClosingEnabled(TypeName, Context.Name);

                return true;
            }

            return false;
        }

        private bool IsTickerAboveTakeProfitRate()
        {
            if (Context.PositionDetails.Orders.Count == 0)
            {
                return false;
            }

            var avgPrice = Context.PositionDetails.Orders.Sum(x => x.Price * x.ExecutedQuantity) / Context.PositionDetails.Orders.Sum(x => x.ExecutedQuantity);
            var takePrice = avgPrice * _options.TakeProfitRate;

            if (Context.Ticker.ClosePrice >= takePrice)
            {
                LogTickerAboveTakeProfitPrice(TypeName, Context.Name, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset, takePrice);

                return true;
            }

            return false;
        }

        private bool IsTickerBelowTrailingStopLoss()
        {
            if (Context.PositionDetails.Orders.Count == 0)
            {
                return false;
            }

            // calculate fixed trailing stop loss based on the last position
            var last = Context.PositionDetails.Orders.Max!.Price;
            var stop = last * _options.TrailingStopLossRate;

            // calculate elastic stop loss if avg position is lower than the last position
            if (_options.ElasticStopLossEnabled)
            {
                var avg = Context.PositionDetails.Orders.Sum(x => x.Price * x.ExecutedQuantity) / Context.PositionDetails.Orders.Sum(x => x.ExecutedQuantity);
                if (avg < last)
                {
                    var mid = avg + ((last - avg) / 2M);
                    stop = Math.Min(stop, mid);
                }
            }

            // evaluate stop loss
            if (Context.Ticker.ClosePrice <= stop)
            {
                return true;
            }

            return false;
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} will signal a buy order for the current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriods}) = {RSI:F8}")]
        private partial void LogWillSignalBuyOrderForCurrentState(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, int rsiPeriods, decimal rsi);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports ticker {Ticker:F8} {Asset} below pullback price of {Pullback:F8} {Asset} = {Indicator}")]
        private partial void LogTickerBelowPullback(string type, string name, decimal ticker, string asset, decimal pullback, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports cooldown period of {Cooldown} since last buy at {LastTime} has passed = {Indicator}")]
        private partial void LogCooldownPeriod(string type, string name, TimeSpan cooldown, DateTime lastTime, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriods}) = {RSI:F8} trending down = {Indicator}")]
        private partial void LogRsiTrendingDown(string type, string name, int rsiPeriods, decimal rsi, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriods}) = {RSI:F8} oversold = {Indicator}")]
        private partial void LogRsiOversold(string type, string name, int rsiPeriods, decimal rsi, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) trending down = {Indicator}")]
        private partial void LogSmaTrendingDown(string type, string name, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Ticker {Ticker:F8} below (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) = {Indicator}")]
        private partial void LogTickerBelowSmas(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) trending up = {Indicator}")]
        private partial void LogSmaTrendingUp(string type, string name, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports Ticker {Ticker:F8} above (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}) = {Indicator}")]
        private partial void LogTickerAboveSmas(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriods}) = {RSI:F8} overbought = {Indicator}")]
        private partial void LogRsiOverbought(string type, string name, int rsiPeriods, decimal rsi, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriods}) = {RSI:F8} trending up = {Indicator}")]
        private partial void LogRsiTrendingUp(string type, string name, int rsiPeriods, decimal rsi, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports buying is disabled")]
        private partial void LogBuyingDisabled(string type, string name);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports accumulation is disabled")]
        private partial void LogAccumulationDisabled(string type, string name);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports selling is disabled")]
        private partial void LogSellingDisabled(string type, string name);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports closing is enabled")]
        private partial void LogClosingEnabled(string type, string name);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports ticker {Ticker:F8} {Asset} is above the take profit price of {Target:F8} {Asset}")]
        private partial void LogTickerAboveTakeProfitPrice(string type, string name, decimal ticker, string asset, decimal target);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} signalling sell for current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriods}) = {RSI:F8})")]
        private partial void LogSignallingSell(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, int rsiPeriods, decimal rsi);

        #endregion Logging
    }
}