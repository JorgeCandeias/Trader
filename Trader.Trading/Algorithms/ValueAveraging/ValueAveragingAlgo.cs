using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.ValueAveraging
{
    internal class ValueAveragingAlgo : SymbolAlgo
    {
        private readonly IAlgoContext _context;
        private readonly IOptionsMonitor<ValueAveragingAlgoOptions> _monitor;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public ValueAveragingAlgo(IAlgoContext context, IOptionsMonitor<ValueAveragingAlgoOptions> monitor, ILogger<ValueAveragingAlgoOptions> logger, ISystemClock clock)
        {
            _context = context;
            _monitor = monitor;
            _logger = logger;
            _clock = clock;
        }

        private static string TypeName => nameof(ValueAveragingAlgo);

        private ValueAveragingAlgoOptions _options = ValueAveragingAlgoOptions.Default;
        private SignificantResult _significant = SignificantResult.Empty;
        private MiniTicker _ticker = MiniTicker.Empty;
        private decimal _smaA;
        private decimal _smaB;
        private decimal _smaC;
        private decimal _rsiA;
        private decimal _rsiB;
        private decimal _rsiC;

        public override async Task<IAlgoResult> GoAsync(CancellationToken cancellationToken = default)
        {
            _options = _monitor.Get(_context.Name);

            // get significant orders
            _significant = await _context.GetSignificantOrderResolver().ResolveAsync(_context.Symbol, cancellationToken);

            // get current ticker
            _ticker = await _context.GetRequiredTickerAsync(_context.Symbol.Name, cancellationToken);

            // calculate current unrealized pnl
            if (_significant.Orders.Count > 0)
            {
                var total = _significant.Orders.Sum(x => x.Price * x.ExecutedQuantity);
                var quantity = _significant.Orders.Sum(x => x.ExecutedQuantity);

                _logger.LogInformation(
                    "{Type} {Name} reports Buy Value = {Total:F8}",
                    TypeName, _context.Name, total);

                var now = quantity * _ticker.ClosePrice;

                _logger.LogInformation(
                    "{Type} {Name} reports Present Value = {Value:F8}",
                    TypeName, _context.Name, now);

                var uPnL = now - total;

                _logger.LogInformation(
                    "{Type} {Name} reports Unrealized PnL = {Value:F8} ({Ratio:P8})",
                    TypeName, _context.Name, uPnL, uPnL / total);

                var rPnl = _significant.Profit.All;

                // this requires the full order set to calculate as a ratio
                _logger.LogInformation(
                    "{Type} {Name} reports Realized PnL = {Value:F8}",
                    TypeName, _context.Name, rPnl, rPnl);

                var pPnl = uPnL + rPnl;

                _logger.LogInformation(
                    "{Type} {Name} reports Present PnL = {Value:F8}",
                    TypeName, _context.Name, pPnl);
            }

            // get the lastest klines
            var maxPeriods = GetMaxPeriods();
            var end = _clock.UtcNow;
            var start = end.Subtract(_options.KlineInterval, maxPeriods);
            var klines = await _context.GetKlinesAsync(_context.Symbol.Name, _options.KlineInterval, start, end, cancellationToken);

            // calculate the current moving averages
            _smaA = klines.LastSimpleMovingAverage(x => x.ClosePrice, _options.SmaPeriodsA);
            _smaB = klines.LastSimpleMovingAverage(x => x.ClosePrice, _options.SmaPeriodsB);
            _smaC = klines.LastSimpleMovingAverage(x => x.ClosePrice, _options.SmaPeriodsC);

            // calculate the rsi values
            _rsiA = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, _options.RsiPeriodsA);
            _rsiB = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, _options.RsiPeriodsB);
            _rsiC = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, _options.RsiPeriodsC);

            if (TrySignalBuyOrder())
            {
                await SetTrackingBuyAsync(_context.Symbol, _options.BuyOrderSafetyRatio, _options.TargetQuoteBalanceFractionPerBuy, _options.MaxNotional, _options.RedeemSavings, cancellationToken);
            }
            else
            {
                await ClearOpenOrdersAsync(_context.Symbol, OrderSide.Buy, cancellationToken);
            }

            if (TrySignalSellOrder())
            {
                await SetSignificantAveragingSellAsync(_context.Symbol, _ticker, _significant.Orders, _options.MinSellProfitRate, _options.RedeemSavings, cancellationToken);
            }
            else
            {
                await ClearOpenOrdersAsync(_context.Symbol, OrderSide.Sell, cancellationToken);
            }

            // publish the profit stats
            await _context.PublishProfitAsync(_significant.Profit);

            return Noop();
        }

        private int GetMaxPeriods()
        {
            return MathS.Max(stackalloc int[] { _options.SmaPeriodsA, _options.SmaPeriodsB, _options.SmaPeriodsC, _options.RsiPeriodsA, _options.RsiPeriodsB, _options.RsiPeriodsC });
        }

        private bool TrySignalBuyOrder()
        {
            // break on disabled opening
            if (_significant.Orders.Count == 0 && !_options.IsOpeningEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} has opening disabled and will not signal a buy order",
                    TypeName, _context.Symbol.Name);

                return false;
            }

            // break on disabled averaging
            if (_significant.Orders.Count > 0 && !_options.IsAveragingEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} has averaging disabled and will not signal a buy order",
                    TypeName, _context.Symbol.Name);

                return false;
            }

            // break on price not low enough from previous significant buy
            if (_significant.Orders.Count > 0)
            {
                // ignore leftovers - only apply this rule if the significant total is above the minimum notional
                var total = _significant.Orders.Sum(x => x.ExecutedQuantity * x.Price);
                if (total >= _context.Symbol.Filters.MinNotional.MinNotional)
                {
                    var minPrice = _significant.Orders.Max!.Price;
                    var lowPrice = minPrice * _options.PullbackRatio;
                    if (_ticker.ClosePrice > lowPrice)
                    {
                        _logger.LogInformation(
                            "{Type} {Symbol} detected ticker of {Ticker:F8} is above the low price of {LowPrice:F8} calculated as {PullBackRatio:F8} of the min significant buy price of {MinPrice:F8} and will not signal a buy order",
                            TypeName, _context.Symbol.Name, _ticker.ClosePrice, lowPrice, _options.PullbackRatio, minPrice);

                        return false;
                    }
                }
            }

            _logger.LogInformation(
                "{Type} {Symbol} evaluating indicators (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8})",
                TypeName, _context.Symbol.Name, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC);

            _logger.LogInformation(
                 "{Type} {Symbol} evaluating indicators (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                 TypeName, _context.Symbol.Name, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);

            // evaluate the signal now
            var isSmaOrdered = _smaA < _smaB && _smaB < _smaC;
            var isTickerUnderSma = _ticker.ClosePrice < _smaA && _ticker.ClosePrice < _smaB && _ticker.ClosePrice < _smaC;
            var isRsiOrdered = _rsiA < _rsiB && _rsiB < _rsiC;
            var isRsiSignaling = _rsiA < _options.RsiOverboughtA && _rsiB < _options.RsiOverboughtB && _rsiC < _options.RsiOverboughtC;
            var signal = isSmaOrdered && isTickerUnderSma && isRsiOrdered && isRsiSignaling;

            if (signal)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} will signal a buy order for the current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                    TypeName, _context.Symbol.Name, _ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);
            }

            return signal;
        }

        private bool TrySignalSellOrder()
        {
            // evaluate target profit rate vs last buy order
            if (_significant.Orders.Max?.Price * _options.TargetSellProfitRate <= _ticker.ClosePrice)
            {
                return true;
            }

            // evaluate indicators
            var isRsiOrdered = _rsiA > _rsiB && _rsiB > _rsiC;
            var isRsiSignaling = _rsiA > _options.RsiOversoldA && _rsiB > _options.RsiOversoldB && _rsiC > _options.RsiOversoldC;
            if (isRsiOrdered && isRsiSignaling)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} signalling sell for current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                    TypeName, _context.Symbol.Name, _ticker.ClosePrice, _options.SmaPeriodsA, _smaA, _options.SmaPeriodsB, _smaB, _options.SmaPeriodsC, _smaC, _options.RsiPeriodsA, _rsiA, _options.RsiPeriodsB, _rsiB, _options.RsiPeriodsC, _rsiC);

                return true;
            }

            return false;
        }
    }
}