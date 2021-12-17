using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal partial class KlineSynchronizer : IKlineSynchronizer
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

    private const string TypeName = nameof(KlineSynchronizer);

    public Task SyncAsync(IEnumerable<(string Symbol, KlineInterval Interval, int Periods)> windows, CancellationToken cancellationToken)
    {
        if (windows is null) throw new ArgumentNullException(nameof(windows));

        return SyncKlinesCoreAsync(windows, cancellationToken);
    }

    private async Task SyncKlinesCoreAsync(IEnumerable<(string Symbol, KlineInterval Interval, int Periods)> windows, CancellationToken cancellationToken)
    {
        LogSyncingKlines(TypeName, windows.Select(x => x.Symbol));

        var watch = Stopwatch.StartNew();

        var end = _clock.UtcNow;

        // pull everything now
        foreach (var item in windows)
        {
            // define the required window
            var start = end.Subtract(item.Interval, item.Periods).AdjustToNext(item.Interval);

            // todo: fix this
            /*
            var synced = await _klines.GetLastSyncedKlineOpenTimeAsync(item.Symbol, item.Interval, cancellationToken);
            if (synced > start)
            {
                start = synced.AdjustToPrevious(item.Interval);
            }
            */

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

                // save the page
                await _klines.SetKlinesAsync(item.Symbol, item.Interval, klines, cancellationToken);

                LogPagedKlines(TypeName, klines.Count, item.Symbol, item.Interval, current, end, total);

                // save the last open time
                var last = klines.Max(x => x.OpenTime);
                await _klines.SetLastSyncedKlineOpenTimeAsync(item.Symbol, item.Interval, last, cancellationToken);

                // break if the page wasnt full
                // using 10 as leeway as binance occasionaly sends complete pages without filling them by one or two items
                if (klines.Count < 990) break;

                // prepare the next page
                current = last.AddMilliseconds(1);
            }
        }

        LogSyncedKlines(TypeName, windows.Select(x => x.Symbol), watch.ElapsedMilliseconds);
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Information, "{Type} is syncing klines for {Symbols}...")]
    private partial void LogSyncingKlines(string type, IEnumerable<string> symbols);

    [LoggerMessage(1, LogLevel.Information, "{Type} paged {Count} klines for {Symbol} {Interval} between {Start} and {End} for a total of {Total} klines")]
    private partial void LogPagedKlines(string type, int count, string symbol, KlineInterval interval, DateTime start, DateTime end, int total);

    [LoggerMessage(2, LogLevel.Information, "{Type} synced klines for {Symbols} in {ElapsedMs}ms...")]
    private partial void LogSyncedKlines(string type, IEnumerable<string> symbols, long elapsedMs);

    #endregion Logging
}