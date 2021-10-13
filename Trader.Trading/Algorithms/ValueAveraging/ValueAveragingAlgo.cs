using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.ValueAveraging
{
    internal class ValueAveragingAlgo : IAlgo
    {
        private readonly IAlgoContext _context;
        private readonly IOptionsMonitor<ValueAveragingAlgoOptions> _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly IKlineProvider _klineProvider;

        public ValueAveragingAlgo(IAlgoContext context, IOptionsMonitor<ValueAveragingAlgoOptions> options, ILogger<ValueAveragingAlgoOptions> logger, ISystemClock clock, IKlineProvider klineProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _klineProvider = klineProvider ?? throw new ArgumentNullException(nameof(klineProvider));
        }

        private static string TypeName => nameof(ValueAveragingAlgo);

        public async Task GoAsync(CancellationToken cancellationToken = default)
        {
            var options = _options.Get(_context.Name);

            var symbol = await _context.TryGetSymbolAsync(options.Symbol).ConfigureAwait(false)
                ?? throw new AlgorithmNotInitializedException();

            _logger.LogInformation("{Type} {Name} running...", TypeName, _context.Name);

            // get significant orders
            var result = await _context.ResolveSignificantOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

            if (await TrySignalBuyOrder(options, symbol, result.Orders, cancellationToken).ConfigureAwait(false))
            {
                await _context
                    .SetTrackingBuyAsync(symbol, options.BuyOrderSafetyRatio, options.TargetQuoteBalanceFractionPerBuy, options.MaxNotional, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _context
                    .ClearOpenOrdersAsync(symbol, OrderSide.Buy, cancellationToken)
                    .ConfigureAwait(false);
            }

            // then place the averaging sell
            await _context
                .SetAveragingSellAsync(symbol, options.ProfitMultipler, options.RedeemSavings, options.SellSavings, cancellationToken)
                .ConfigureAwait(false);

            // publish the profit stats
            await _context
                .PublishProfitAsync(result.Profit)
                .ConfigureAwait(false);
        }

        private async ValueTask<bool> TrySignalBuyOrder(ValueAveragingAlgoOptions options, Symbol symbol, ImmutableSortedOrderSet orders, CancellationToken cancellationToken)
        {
            // evaluate the orders vs config for negative signals
            if (orders.Count == 0 && !options.IsOpeningEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} has opening disabled and will not signal a buy order",
                    TypeName, symbol.Name);

                return false;
            }
            else if (orders.Count > 0 && !options.IsAveragingEnabled)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} has averaging disabled and will not signal a buy order",
                    TypeName, symbol.Name);

                return false;
            }

            // get current ticker
            var ticker = await _context.TryGetTickerAsync(symbol.Name, cancellationToken).ConfigureAwait(false)
                ?? throw new AlgorithmNotInitializedException($"Could not get ticker for '{symbol.Name}'");

            // evaluate ticker vs minimum past price
            if (orders.Count > 0)
            {
                var minPrice = orders.Min(x => x.Price);
                var lowPrice = minPrice * options.PullbackRatio;
                if (ticker.ClosePrice > lowPrice)
                {
                    _logger.LogInformation(
                        "{Type} {Symbol} detected ticker of {Ticker:F8} is above the low price of {LowPrice:F8} calculated as {PullBackRatio:F8} of the min significant buy price of {MinPrice:F8} and will not signal a buy order",
                        TypeName, symbol.Name, ticker.ClosePrice, lowPrice, options.PullbackRatio, minPrice);

                    return false;
                }
            }

            // get the lastest klines
            var end = _clock.UtcNow;
            var start = end.Subtract(TimeSpan.FromDays(100));
            var klines = await _klineProvider.GetKlinesAsync(symbol.Name, KlineInterval.Days1, start, end, cancellationToken).ConfigureAwait(false);

            // calculate the current moving averages
            var smaA = klines.LastSimpleMovingAverage(x => x.ClosePrice, options.SmaPeriodsA);
            var smaB = klines.LastSimpleMovingAverage(x => x.ClosePrice, options.SmaPeriodsB);
            var smaC = klines.LastSimpleMovingAverage(x => x.ClosePrice, options.SmaPeriodsC);

            _logger.LogInformation(
                "{Type} {Symbol} evaluating indicators (SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8})",
                TypeName, symbol.Name, options.SmaPeriodsA, smaA, options.SmaPeriodsB, smaB, options.SmaPeriodsC, smaC);

            // evaluate the smas for negative signals
            if (smaA > smaB)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected SMA({SmaPeriodsA}) of {SMAA:F8} is greater than SMA({SmaPeriodsB}) of {SMAB:F8} and will not signal a buy order",
                    TypeName, symbol.Name, options.SmaPeriodsA, smaA, options.SmaPeriodsB, smaB);

                return false;
            }
            else if (smaB > smaC)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected SMA({SmaPeriodsB}) of {SMAB:F8} is greater than SMA({SmaPeriodsC}) of {SMAC:F8} and will not signal a buy order",
                    TypeName, symbol.Name, options.SmaPeriodsB, smaB, options.SmaPeriodsC, smaC);

                return false;
            }

            // evaluate the ticker vs smas for negative signals
            if (ticker.ClosePrice >= smaA)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected ticker of {Ticker:F8} is greater than or equal to SMA({SmaPeriodsA}) of {SMAA:F8} and will not signal a buy order",
                    TypeName, symbol.Name, ticker, options.SmaPeriodsA, smaA);

                return false;
            }
            else if (ticker.ClosePrice >= smaB)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected ticker of {Ticker:F8} is greater than or equal to SMB({SmaPeriodsB}) of {SMAB:F8} and will not signal a buy order",
                    TypeName, symbol.Name, ticker, options.SmaPeriodsB, smaB);

                return false;
            }
            else if (ticker.ClosePrice >= smaC)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected ticker of {Ticker:F8} is greater than or equal to SMB({SmaPeriodsC}) of {SMAC:F8} and will not signal a buy order",
                    TypeName, symbol.Name, ticker, options.SmaPeriodsC, smaC);

                return false;
            }

            // calculate the rsi values
            var rsiA = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, options.RsiPeriodsA);
            var rsiB = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, options.RsiPeriodsB);
            var rsiC = klines.LastRelativeStrengthIndexOrDefault(x => x.ClosePrice, options.RsiPeriodsC);

            _logger.LogInformation(
                "{Type} {Symbol} evaluating indicators (RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                TypeName, symbol.Name, options.RsiPeriodsA, rsiA, options.RsiPeriodsB, rsiB, options.RsiPeriodsC, rsiC);

            // evaluate the rsi waves against each other
            if (rsiA > rsiB)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected RSI({RsiPeriodsA}) of {RSIA:F8} is greater than or equal to RSI({RsiPeriodsB}) of {RSIB:F8} and will not signal a buy order",
                    TypeName, symbol.Name, options.RsiPeriodsA, rsiA, options.RsiPeriodsB, rsiB);

                return false;
            }
            else if (rsiB > rsiC)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected RSI({RsiPeriodsB}) of {RSIB:F8} is greater than or equal to RSI({RsiPeriodsC}) of {RSIC:F8} and will not signal a buy order",
                    TypeName, symbol.Name, options.RsiPeriodsB, rsiB, options.RsiPeriodsC, rsiC);

                return false;
            }

            // evaluate the rsi values for negative signals
            if (rsiA > options.RsiOverboughtA)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected RSI({RsiPeriodsA}) of {RSIA:F8} is greater than overbought signal of {OverboughtA:F8} and will not signal a buy order",
                    TypeName, symbol.Name, options.RsiPeriodsA, rsiA, options.RsiOverboughtA);

                return false;
            }
            else if (rsiB > options.RsiOverboughtB)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected RSI({RsiPeriodsB}) of {RSIB:F8} is greater than overbought signal of {OverboughtB:F8} and will not signal a buy order",
                    TypeName, symbol.Name, options.RsiPeriodsB, rsiB, options.RsiOverboughtB);

                return false;
            }
            else if (rsiC > options.RsiOverboughtC)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} detected RSI({RsiPeriodsC}) of {RSIC:F8} is greater than overbought signal of {OverboughtC:F8} and will not signal a buy order",
                    TypeName, symbol.Name, options.RsiPeriodsC, rsiC, options.RsiOverboughtC);

                return false;
            }

            // if we reached here then we have a buy signal
            _logger.LogInformation(
                "{Type} {Symbol} will signal a buy order for the current state (Ticker = {Ticker:F8}, SMA({SmaPeriodsA}) = {SMAA:F8}, SMA({SmaPeriodsB}) = {SMAB:F8}, SMA({SmaPeriodsC}) = {SMAC:F8}, RSI({RsiPeriodsA}) = {RSIA:F8}, RSI({RsiPeriodsB}) = {RSIB:F8}, RSI({RsiPeriodsC}) = {RSIC:F8})",
                TypeName, symbol.Name, ticker.ClosePrice, options.SmaPeriodsA, smaA, options.SmaPeriodsB, smaB, options.SmaPeriodsC, smaC, options.RsiPeriodsA, rsiA, options.RsiPeriodsB, rsiB, options.RsiPeriodsC, rsiC);

            return true;
        }
    }
}