using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class BinanceMarketDataGrain : Grain, IBinanceMarketDataGrain
    {
        private readonly ILogger _logger;
        private readonly IMarketDataStreamClientFactory _factory;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly IMapper _mapper;
        private readonly ISystemClock _clock;
        private readonly IHostApplicationLifetime _lifetime;

        private readonly HashSet<string> _tickerSymbols;
        private readonly Dictionary<(string Symbol, KlineInterval Interval), int> _klineWindows;

        public BinanceMarketDataGrain(ILogger<BinanceMarketDataGrain> logger, IMarketDataStreamClientFactory factory, ITradingRepository repository, ITradingService trader, IMapper mapper, ISystemClock clock, IAlgoDependencyInfo dependencies, IHostApplicationLifetime lifetime)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _tickerSymbols = dependencies.GetTickers().ToHashSet(StringComparer.OrdinalIgnoreCase);

            _klineWindows = dependencies
                .GetKlines()
                .GroupBy(x => (x.Symbol, x.Interval))
                .Select(x => (x.Key, Periods: x.Max(y => y.Periods)))
                .ToDictionary(x => x.Key, x => x.Periods);
        }

        private static string TypeName => nameof(BinanceMarketDataGrain);

        /// <summary>
        /// Holds the background streaming and syncing work.
        /// </summary>
        private Task? _work;

        /// <summary>
        /// Conflates the incoming tickers from the background stream so the repository has a chance to keep up.
        /// </summary>
        private readonly ConcurrentDictionary<string, (MiniTicker Ticker, bool Saved)> _tickers = new();

        /// <summary>
        /// Conflates the incoming klines from the background stream so the repository has a chance to keep up.
        /// </summary>
        private readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval, DateTime OpenTime), (Kline Kline, bool Saved)> _klines = new();

        /// <summary>
        /// Keeps track of readyness state.
        /// </summary>
        private bool _ready;

        public override Task OnActivateAsync()
        {
            // if there are ticker or kline dependencies then ensure we keep streaming them
            if (_tickerSymbols.Count > 0 || _klineWindows.Count > 0)
            {
                RegisterTimer(TickEnsureStreamAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            // if there are tickers to manage then ensure we keep saving them to the repository
            if (_tickerSymbols.Count > 0)
            {
                RegisterTimer(TickSaveTickersAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            // if there are klines to manage then ensure we keep saving them to the repository
            if (_klineWindows.Count > 0)
            {
                RegisterTimer(TickSaveKlinesAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                RegisterTimer(TickClearKlinesAsync, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }

            return base.OnActivateAsync();
        }

        /// <summary>
        /// Monitors the background streaming work task and ensures it remains active upon faulting.
        /// </summary>
        private async Task TickEnsureStreamAsync(object _)
        {
            // avoid starting streaming work upon shutdown
            if (_lifetime.ApplicationStopping.IsCancellationRequested) return;

            // schedule streaming work if nothing is running
            if (_work is null)
            {
                _work = Task.Run(ExecuteStreamAsync, _lifetime.ApplicationStopping);
                return;
            }

            // propagate any exceptions from completed streaming work and release the task
            if (_work.IsCompleted)
            {
                try
                {
                    await _work;
                }
                finally
                {
                    _work = null;
                }
            }
        }

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "N/A")]
        private async Task ExecuteStreamAsync()
        {
            try
            {
                // this helps cancel every local step upon stream failure at any point
                using var localCancellation = new CancellationTokenSource();
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(localCancellation.Token, _lifetime.ApplicationStopping);

                // create a client for the streams we want
                var streams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                streams.UnionWith(_tickerSymbols.Select(x => $"{x.ToLowerInvariant()}@miniTicker"));
                streams.UnionWith(_klineWindows.Select(x => $"{x.Key.Symbol.ToLowerInvariant()}@kline_{_mapper.Map<string>(x.Key.Interval)}"));

                _logger.LogInformation("{Name} connecting to streams {Streams}...", TypeName, streams);

                using var client = _factory.Create(streams);

                await client.ConnectAsync(linkedCancellation.Token);

                // start streaming in the background while we sync from the api
                // we use the activation scheduler for this background task so that we can access grain state in a concurrency safe manner
                var streamTask = Task.Run(async () =>
                {
                    while (!linkedCancellation.Token.IsCancellationRequested)
                    {
                        var message = await client.ReceiveAsync(linkedCancellation.Token);

                        if (message.Error is not null)
                        {
                            throw new BinanceCodeException(message.Error.Code, message.Error.Message, 0);
                        }

                        if (message.MiniTicker is not null && _tickerSymbols.Contains(message.MiniTicker.Symbol))
                        {
                            SetTicker(message.MiniTicker);
                        }

                        if (message.Kline is not null && _klineWindows.ContainsKey((message.Kline.Symbol, message.Kline.Interval)))
                        {
                            SetKline(message.Kline);
                        }
                    }
                }, linkedCancellation.Token);

                // sync tickers from the api
                await SyncTickersAsync(linkedCancellation.Token);

                // sync klines from the api
                await SyncKlinesAsync(linkedCancellation.Token);

                // signal the ready state to allow algos to execute
                _ready = true;

                // keep streaming now
                await streamTask;
            }
            finally
            {
                _ready = false;
            }
        }

        /// <summary>
        /// Syncs tickers from binance into the conflation cache.
        /// </summary>
        private async Task SyncTickersAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is syncing tickers for {Symbols}...", TypeName, _tickerSymbols);
            var watch = Stopwatch.StartNew();

            foreach (var symbol in _tickerSymbols)
            {
                var subWatch = Stopwatch.StartNew();

                var result = await _trader
                    .WithBackoff()
                    .Get24hTickerPriceChangeStatisticsAsync(symbol, cancellationToken);

                var ticker = _mapper.Map<MiniTicker>(result);

                // conflate the ticker if it is newer than the cached one - otherwise discard it
                SetTicker(ticker);

                _logger.LogInformation("{Name} synced ticker for {Symbol} in {ElapsedMs}ms", TypeName, symbol, subWatch.ElapsedMilliseconds);
            }

            _logger.LogInformation("{Name} synced tickers for {Symbols} in {ElapsedMs}ms", TypeName, _tickerSymbols, watch.ElapsedMilliseconds);
        }

        private async Task SyncKlinesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is syncing klines for {Symbols}...", TypeName, _klineWindows.Select(x => x.Key.Symbol));
            var watch = Stopwatch.StartNew();

            var end = _clock.UtcNow;

            foreach (var item in _klineWindows)
            {
                // define the required window
                var start = end.Subtract(item.Key.Interval, item.Value).AdjustToNext(item.Key.Interval);

                // discover the first kline missing
                var missing = await TryGetFirstMissingKlineAsync(item.Key.Symbol, item.Key.Interval, start, end, cancellationToken);

                // skip this dependency if we already have all the data
                if (missing is null)
                {
                    continue;
                }
                else
                {
                    start = missing.Value;
                }

                // start syncing from the first missing kline
                var current = start;
                var total = 0;

                while (current < end)
                {
                    // query a kline page from the exchange
                    var klines = await _trader
                        .WithBackoff()
                        .GetKlinesAsync(item.Key.Symbol, item.Key.Interval, current, end, 1000, cancellationToken);

                    // break if the page is empty
                    if (klines.Count is 0)
                    {
                        break;
                    }
                    else
                    {
                        total += klines.Count;
                    }

                    // save all the klines in the page
                    foreach (var kline in klines)
                    {
                        SetKline(kline);
                    }

                    _logger.LogInformation(
                        "{Name} paged {Count} klines for {Symbol} {Interval} between {Start} and {End} for a total of {Total}",
                        TypeName, klines.Count, item.Key.Symbol, item.Key.Interval, current, end, total);

                    // break if the page wasnt full
                    // using 10 as leeway as binance occasionaly sends complete pages without filling them by one or two items
                    if (klines.Count < 990) break;

                    // prepare the next page
                    current = klines.Max(x => x.OpenTime).AddMilliseconds(1);
                }
            }

            _logger.LogInformation("{Name} synced klines for {Symbols} in {ElapsedMs}ms...", TypeName, _klineWindows.Select(x => x.Key.Symbol), watch.ElapsedMilliseconds);
        }

        private async Task<DateTime?> TryGetFirstMissingKlineAsync(string symbol, KlineInterval interval, DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            // load existing klines from repository
            var saved = await _repository.GetKlinesAsync(symbol, interval, start, end, cancellationToken);
            var times = saved.Where(x => x.IsClosed).Select(x => x.OpenTime).ToHashSet();

            // discover the first kline missing
            foreach (var time in interval.Range(start, end))
            {
                if (!times.Contains(time))
                {
                    return time;
                }
            }

            return null;
        }

        /// <summary>
        /// Saves conflated tickers to the repository.
        /// </summary>
        private async Task TickSaveTickersAsync(object _)
        {
            // avoid ticking on application shutdown
            if (_lifetime.ApplicationStopping.IsCancellationRequested) return;

            var buffer = ArrayPool<MiniTicker>.Shared.Rent(_tickerSymbols.Count);
            var count = 0;

            // stage the unsaved tickers from the conflation
            foreach (var item in _tickers.Select(x => x.Value))
            {
                if (count >= buffer.Length)
                {
                    break;
                }

                if (!item.Saved)
                {
                    buffer[count++] = item.Ticker;
                }
            }

            // break if there is nothing to save
            if (count <= 0)
            {
                return;
            }

            // save all tickers in one go
            await _repository.SetTickersAsync(buffer.Take(count), _lifetime.ApplicationStopping);

            // mark unchanged saved tickers in the concurrent conflation to avoid saving them again
            for (var i = 0; i < count; i++)
            {
                var ticker = buffer[i];

                // the concurrent dictionary may have changed in the background so we must avoid marking newer data as saved
                _tickers.TryUpdate(ticker.Symbol, (ticker, true), (ticker, false));
            }

            ArrayPool<MiniTicker>.Shared.Return(buffer);
        }

        /// <summary>
        /// Saves conflated klines to the repository.
        /// </summary>
        private async Task TickSaveKlinesAsync(object _)
        {
            // avoid ticking on application shutdown
            if (_lifetime.ApplicationStopping.IsCancellationRequested) return;

            var buffer = ArrayPool<Kline>.Shared.Rent(_klines.Count);
            var count = 0;

            // stage the unsaved klines from the concurrent conflation
            foreach (var item in _klines.Select(x => x.Value))
            {
                // break if the buffer is full
                if (count >= buffer.Length) break;

                // skip saved items
                if (item.Saved) continue;

                // stage unsaved items
                buffer[count++] = item.Kline;
            }

            // save all klines to the repository
            if (count > 0)
            {
                await _repository.SetKlinesAsync(buffer.Take(count), _lifetime.ApplicationStopping);

                // mark unsaved klines as saved to avoid saving them again
                for (var i = 0; i < count; i++)
                {
                    var kline = buffer[i];

                    // only mark the kline saved if it hasn't been removed by the clearing timer
                    _klines.TryUpdate((kline.Symbol, kline.Interval, kline.OpenTime), (kline, true), (kline, false));
                }
            }

            ArrayPool<Kline>.Shared.Return(buffer);
        }

        /// <summary>
        /// Clears old klines from memory.
        /// </summary>
        private Task TickClearKlinesAsync(object _)
        {
            // avoid ticking on application shutdown
            if (_lifetime.ApplicationStopping.IsCancellationRequested) return Task.CompletedTask;

            var buffer = ArrayPool<Kline>.Shared.Rent(_klines.Count);
            var count = 0;

            // elect expired saved klines for removal
            var now = _clock.UtcNow;
            foreach (var item in _klines)
            {
                // protect against concurrent dictionary size increasing during enumeration
                if (count >= buffer.Length) break;

                // attempt to elect the item for removal
                if (_klineWindows.TryGetValue((item.Key.Symbol, item.Key.Interval), out var periods) && item.Key.OpenTime < now.Subtract(item.Key.Interval, periods) && item.Value.Saved)
                {
                    buffer[count++] = item.Value.Kline;
                }
            }

            // safely remove the elected items
            for (var i = 0; i < count; i++)
            {
                var item = buffer[i];

                _klines.TryRemove(new KeyValuePair<(string, KlineInterval, DateTime), (Kline, bool)>((item.Symbol, item.Interval, item.OpenTime), (item, true)));
            }

            ArrayPool<Kline>.Shared.Return(buffer);

            return Task.CompletedTask;
        }

        public Task<bool> IsReadyAsync() => Task.FromResult(_ready);

        public ValueTask<MiniTicker?> TryGetTickerAsync(string symbol)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            if (_tickers.TryGetValue(symbol, out var value))
            {
                return ValueTask.FromResult<MiniTicker?>(value.Ticker);
            }
            else
            {
                return ValueTask.FromResult<MiniTicker?>(null);
            }
        }

        /// <summary>
        /// Adds the specified kline to the cache.
        /// </summary>
        private void SetKline(Kline kline)
        {
            // keep the kline if brand new or newer than existing one
            _klines.AddOrUpdate(
                (kline.Symbol, kline.Interval, kline.OpenTime),
                (key, arg) => arg,
                (key, current, arg) => arg.kline.EventTime > current.Kline.EventTime ? arg : current,
                (kline, false));
        }

        /// <summary>
        /// Adds the specified ticker to the cache.
        /// </summary>
        private void SetTicker(MiniTicker ticker)
        {
            // keep the ticker if brand new or newer than existing one
            _tickers.AddOrUpdate(
                ticker.Symbol,
                (key, arg) => arg,
                (key, current, arg) => arg.ticker.EventTime > current.Ticker.EventTime ? arg : current,
                (ticker, false));
        }
    }
}