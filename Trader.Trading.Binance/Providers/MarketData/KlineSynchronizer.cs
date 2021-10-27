using Microsoft.Extensions.Logging;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class KlineSynchronizer : IKlineSynchronizer
    {
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;
        private readonly IKlineProvider _klines;
        private readonly ITradingService _trader;

        public KlineSynchronizer(ILogger<KlineSynchronizer> logger, ISystemClock clock, IKlineProvider klines, ITradingService trader)
        {
            _logger = logger;
            _clock = clock;
            _klines = klines;
            _trader = trader;
        }

        private static string TypeName => nameof(KlineSynchronizer);

        public Task SyncAsync(IEnumerable<(string Symbol, KlineInterval Interval, int Periods)> windows, CancellationToken cancellationToken)
        {
            if (windows is null) throw new ArgumentNullException(nameof(windows));

            return SyncKlinesCoreAsync(windows, cancellationToken);
        }

        private async Task SyncKlinesCoreAsync(IEnumerable<(string Symbol, KlineInterval Interval, int Periods)> windows, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is syncing klines for {Symbols}...", TypeName, windows.Select(x => x.Symbol));

            var watch = Stopwatch.StartNew();

            var end = _clock.UtcNow;

            // batches saving work in the background so we can keep pulling data without waiting
            var work = new ActionBlock<(string Symbol, KlineInterval Interval, IEnumerable<Kline> Items)>(item => _klines.SetKlinesAsync(item.Symbol, item.Interval, item.Items));

            // pull everything now
            foreach (var item in windows)
            {
                // define the required window
                var start = end.Subtract(item.Interval, item.Periods).AdjustToNext(item.Interval);

                // start syncing from the first missing kline
                var current = start;
                var total = 0;

                while (current < end)
                {
                    // query a kline page from the exchange
                    var klines = await _trader
                        .WithBackoff()
                        .GetKlinesAsync(item.Symbol, item.Interval, current, end, 1000, cancellationToken);

                    // break if the page is empty
                    if (klines.Count is 0)
                    {
                        break;
                    }
                    else
                    {
                        total += klines.Count;
                    }

                    // queue the page for saving
                    work.Post((item.Symbol, item.Interval, klines));

                    _logger.LogInformation(
                        "{Name} paged {Count} klines for {Symbol} {Interval} between {Start} and {End} for a total of {Total} klines",
                        TypeName, klines.Count, item.Symbol, item.Interval, current, end, total);

                    // break if the page wasnt full
                    // using 10 as leeway as binance occasionaly sends complete pages without filling them by one or two items
                    if (klines.Count < 990) break;

                    // prepare the next page
                    current = klines.Max(x => x.OpenTime).AddMilliseconds(1);
                }
            }

            // wait for background saving to complete now
            work.Complete();
            await work.Completion;

            _logger.LogInformation(
                "{Name} synced klines for {Symbols} in {ElapsedMs}ms...",
                TypeName, windows.Select(x => x.Symbol), watch.ElapsedMilliseconds);
        }
    }
}