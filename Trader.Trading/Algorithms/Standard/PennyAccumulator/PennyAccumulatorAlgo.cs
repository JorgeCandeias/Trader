using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.PennyAccumulator
{
    [ExcludeFromCodeCoverage]
    internal class PennyAccumulatorAlgo : Algo
    {
        private readonly IOptionsMonitor<PennyAccumulatorOptions> _options;
        private readonly IOptionsMonitor<AlgoDependencyOptions> _dependencies;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public PennyAccumulatorAlgo(IOptionsMonitor<PennyAccumulatorOptions> options, IOptionsMonitor<AlgoDependencyOptions> dependencies, ILogger<PennyAccumulatorAlgo> logger, ITradingService trader, ISystemClock clock)
        {
            _options = options;
            _dependencies = dependencies;
            _logger = logger;
            _trader = trader;
            _clock = clock;
        }

        private static string TypeName => nameof(PennyAccumulatorAlgo);

        public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            // fetch all tickers
            var tickers = await _trader
                .Get24hTickerPriceChangeStatisticsAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "{Name} got {Count} tickers", TypeName, tickers.Count);

            // attempt to ignore symbols from other algos
            var others = _dependencies.CurrentValue.Symbols;

            _logger.LogInformation(
                "{Name} ignoring {Count} symbols from other algos",
                TypeName, others.Count);

            // select the lowest n prices to look at
            var window = _clock.UtcNow.AddDays(-1);
            var lowest = tickers

                // look at live tickers only
                .Where(x => x.FirstId > 0 && x.LastId > 0 && x.Volume > 0m && x.CloseTime > window)

                // look at symbols for this algo quote asset only
                .Where(x => x.Symbol.EndsWith(_options.CurrentValue.QuoteAsset, StringComparison.Ordinal))

                // look at symbols that are not managed by other algos only
                .Where(x => !others.Contains(x.Symbol))

                // look at the lowest by price
                .OrderBy(x => x.LastPrice)

                // look at the lowest n symbols only
                .Take(_options.CurrentValue.MaxAssetCount)
                .ToHashSet();

            _logger.LogInformation(
                "{Name} selected {Count} lowest price tickers for quote asset {Quote}",
                TypeName, lowest.Count, _options.CurrentValue.QuoteAsset);

            // get last trade for each ticker and discard those on cooloff
            await RemoveTickersOnCooloffAsync(lowest, cancellationToken);

            // get rsi for each ticker and discard the ones that are not signaling
            await RemoveTickersWithHighRsiAsync(lowest, cancellationToken);

            // select the lowest price candidate
            var elected = lowest.OrderBy(x => x.LastPrice).FirstOrDefault();
            if (elected is not null)
            {
                _logger.LogInformation(
                    "{Name} elected symbol {Symbol} with price {Price:F8} for buying",
                    TypeName, elected.Symbol, elected.LastPrice);

                var symbol = await Context.GetRequiredSymbolAsync(elected.Symbol, cancellationToken);

                //return TrackingBuy(symbol, 0.999m, 0.001m, 0.00020000m, true);
                return Noop();
            }

            return Noop();
        }

        private async Task RemoveTickersWithHighRsiAsync(HashSet<Ticker> tickers, CancellationToken cancellationToken)
        {
            var excluded = new HashSet<Ticker>();
            foreach (var ticker in tickers)
            {
                var end = _clock.UtcNow.AdjustToNext(_options.CurrentValue.RsiInterval);
                var start = end.Subtract(_options.CurrentValue.RsiInterval, 1000);

                var klines = await _trader.GetKlinesAsync(ticker.Symbol, _options.CurrentValue.RsiInterval, start, end, 1000, cancellationToken);

                _logger.LogInformation(
                    "{Name} got {Count} klines for symbol {Symbol}",
                    TypeName, klines.Count, ticker.Symbol);

                var rsis = klines.Rsi(x => x.ClosePrice, 14).ToList();

                _logger.LogInformation("RSI(14) over 14 = {RSI}", klines.TakeLast(14).Rsi(x => x.ClosePrice, 14).TakeLast(14));
                _logger.LogInformation("RSI(14) over 20 = {RSI}", klines.TakeLast(20).Rsi(x => x.ClosePrice, 14).TakeLast(14));
                _logger.LogInformation("RSI(14) over 100 = {RSI}", klines.TakeLast(100).Rsi(x => x.ClosePrice, 14).TakeLast(14));
                _logger.LogInformation("RSI(14) over 200 = {RSI}", klines.TakeLast(200).Rsi(x => x.ClosePrice, 14).TakeLast(14));
                _logger.LogInformation("RSI(14) over {Count} = {RSI}", klines.Count, klines.Rsi(x => x.ClosePrice, 14).TakeLast(14));

                var rsi = klines.LastRsi(x => x.ClosePrice, _options.CurrentValue.RsiPeriods);

                _logger.LogInformation(
                    "{Name} reports symbol {Symbol} with price {Price:F8} has RSI of {Rsi:F8}",
                    TypeName, ticker.Symbol, ticker.LastPrice, rsi);

                if (rsi > _options.CurrentValue.RsiOversold)
                {
                    excluded.Add(ticker);

                    _logger.LogInformation(
                        "{Name} discarded symbol {Symbol} with price {Price:F8} and RSI of {Rsi:F8}",
                        TypeName, ticker.Symbol, ticker.LastPrice, rsi);
                }
            }
            tickers.ExceptWith(excluded);

            _logger.LogInformation(
                "{Name} excluded {Count} symbols with RSI not low enough",
                TypeName, excluded.Count);
        }

        private async Task RemoveTickersOnCooloffAsync(HashSet<Ticker> tickers, CancellationToken cancellationToken)
        {
            var excluded = new HashSet<Ticker>();
            foreach (var ticker in tickers)
            {
                var trades = await _trader.GetAccountTradesAsync(ticker.Symbol, null, 1, cancellationToken);
                var trade = trades.SingleOrDefault();

                if (trade is not null && trade.Time.Add(_options.CurrentValue.CooloffPeriod) > _clock.UtcNow)
                {
                    excluded.Add(ticker);
                }
            }
            tickers.ExceptWith(excluded);

            _logger.LogInformation(
                "{Name} excluded {Count} symbols still within cool off time",
                TypeName, excluded.Count);
        }
    }
}