using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;

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
        private decimal _rsiA;
        private decimal _rsiB;
        private decimal _rsiC;

        protected override async ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            // calculate the current moving averages
            _smaA = Context.Klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsA);
            _smaB = Context.Klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsB);
            _smaC = Context.Klines.LastSma(x => x.ClosePrice, _options.SmaPeriodsC);

            // calculate the rsi values
            _rsiA = Context.Klines.LastRsi(x => x.ClosePrice, _options.RsiPeriodsA);
            _rsiB = Context.Klines.LastRsi(x => x.ClosePrice, _options.RsiPeriodsB);
            _rsiC = Context.Klines.LastRsi(x => x.ClosePrice, _options.RsiPeriodsC);

            // decide on buying
            if (await TrySignalBuyOrder())
            {
                return Many(
                    ClearOpenOrders(OrderSide.Sell),
                    SetTrackingBuy());
            }

            // decide on selling
            if (TrySignalSellOrder())
            {
                return Many(
                    ClearOpenOrders(OrderSide.Buy),
                    SignificantAveragingSell(Context.Ticker, Context.PositionDetails.Orders, _options.MinSellProfitRate, _options.RedeemSavings, _options.RedeemSwapPool));
                //MarketSell(Context.Symbol, Context.PositionDetails.Orders.Sum(x => x.ExecutedQuantity), _options.RedeemSavings, _options.RedeemSwapPool));
            }

            return Many(ClearOpenOrders(OrderSide.Buy), ClearOpenOrders(OrderSide.Sell));
        }

        private async ValueTask<bool> TrySignalBuyOrder()
        {
            if (!IsBuyingEnabled()) return false;

            if (IsCooled() && IsRsiOversold()) return true;

            if (await IsTickerOnNextStep()) return true;

            return false;

            /*
            return
                IsBuyingEnabled() &&
                IsCooled() &&
                //IsBelowPullbackPrice() &&
                //IsSmaTrendingDown() &&
                //IsTickerBelowSmas() &&
                //IsRsiTrendingDown() &&
                IsRsiOversold();
            */
        }

        private IAlgoCommand SetTrackingBuy()
        {
            LogWillSignalBuyOrderForCurrentState(TypeName, Context.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);

            return TrackingBuy(Context.Symbol, _options.BuyOrderSafetyRatio, _options.BuyQuoteBalanceFraction, _options.MaxNotional, _options.RedeemSavings, _options.RedeemSwapPool);
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
            var last = trades.LastOrDefault();
            if (!(last is not null && last.IsBuyer))
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

            LogRsiTrendingDown(TypeName, Context.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

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

            LogRsiTrendingUp(TypeName, Context.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC, indicator);

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

            // calculate fixed stop loss based on the last position
            var last = Context.PositionDetails.Orders.Max!.Price;
            var stop = last * _options.TrailingStopLossRate;

            // calculate elastic stop loss if avg position is lower than the last position
            var avg = Context.PositionDetails.Orders.Sum(x => x.Price * x.ExecutedQuantity) / Context.PositionDetails.Orders.Sum(x => x.ExecutedQuantity);
            if (avg < last)
            {
                var mid = avg + ((last - avg) / 2M);
                stop = Math.Min(stop, mid);
            }

            // evaluate stop loss
            if (Context.Ticker.ClosePrice <= stop)
            {
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

            if (IsClosingEnabled() || IsTickerAboveTakeProfitRate() || IsTickerBelowTrailingStopLoss())
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
                LogSignallingSell(TypeName, Context.Name, Context.Ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);
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
        private partial void LogRsiTrendingDown(string type, string name, int rsiPeriodsA, decimal rsiA, int rsiPeriodsB, decimal rsiB, int rsiPeriodsC, decimal rsiC, bool indicator);

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

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8}) trending up = {Indicator}")]
        private partial void LogRsiTrendingUp(string type, string name, int rsiPeriodsA, decimal rsiA, int rsiPeriodsB, decimal rsiB, int rsiPeriodsC, decimal rsiC, bool indicator);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports buying is disabled")]
        private partial void LogBuyingDisabled(string type, string name);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports selling is disabled")]
        private partial void LogSellingDisabled(string type, string name);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports closing is enabled")]
        private partial void LogClosingEnabled(string type, string name);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} reports ticker {Ticker:F8} {Asset} is above the take profit price of {Target:F8} {Asset}")]
        private partial void LogTickerAboveTakeProfitPrice(string type, string name, decimal ticker, string asset, decimal target);

        [LoggerMessage(0, LogLevel.Information, "{Type} {Name} signalling sell for current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})")]
        private partial void LogSignallingSell(string type, string name, decimal ticker, int smaPeriodsA, decimal smaA, int smaPeriodsB, decimal smaB, int smaPeriodsC, decimal smaC, int rsiPeriodsA, decimal rsiA, int rsiPeriodsB, decimal rsiB, int rsiPeriodsC, decimal rsiC);

        #endregion Logging
    }
}