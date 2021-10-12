using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class BinanceMarketDataGrain : Grain, IBinanceMarketDataGrain
    {
        private readonly BinanceOptions _options;
        private readonly ILogger _logger;
        private readonly IMarketDataStreamClientFactory _factory;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly IMapper _mapper;
        private readonly ISystemClock _clock;

        private readonly HashSet<string> _tickerSymbols;
        private readonly Dictionary<(string Symbol, KlineInterval Interval), TimeSpan> _klineWindows;

        public BinanceMarketDataGrain(IOptions<BinanceOptions> options, ILogger<BinanceMarketDataGrain> logger, IMarketDataStreamClientFactory factory, ITradingRepository repository, ITradingService trader, IMapper mapper, ISystemClock clock, IAlgoDependencyInfo dependencies)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _tickerSymbols = dependencies.GetTickers().ToHashSet(StringComparer.OrdinalIgnoreCase);

            _klineWindows = dependencies
                .GetKlines()
                .GroupBy(x => (x.Symbol, x.Interval))
                .Select(x => (x.Key, Window: x.Max(y => y.Window)))
                .ToDictionary(x => x.Key, x => x.Window);
        }

        private static string TypeName => nameof(BinanceMarketDataGrain);

        /// <summary>
        /// Helps cancel background work upon grain deactivation.
        /// </summary>
        private readonly CancellationTokenSource _cancellation = new();

        /// <summary>
        /// Holds the background streaming and syncing work.
        /// </summary>
        private Task? _work;

        /// <summary>
        /// Conflates the incoming tickers from the background stream so the repository has a chance to keep up.
        /// </summary>
        private readonly ConcurrentDictionary<string, (MiniTicker Ticker, Guid Version, bool Saved)> _tickers = new();

        /// <summary>
        /// Tracks completion requests for inbound reactive polls.
        /// </summary>
        private readonly ConcurrentDictionary<string, TaskCompletionSource<(MiniTicker?, Guid)>> _tickerCompletions = new();

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
            RegisterTimer(_ => TickEnsureWorkAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            if (_tickerSymbols.Count > 0)
            {
                RegisterTimer(_ => TickSaveTickersAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            if (_klineWindows.Count > 0)
            {
                RegisterTimer(_ => TickSaveKlinesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                RegisterTimer(_ => TickClearKlinesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Cancel();

            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// Monitors the background streaming work task and ensures it remains active upon faulting.
        /// </summary>
        private async Task TickEnsureWorkAsync()
        {
            // avoid starting streaming work upon shutdown
            if (_cancellation.IsCancellationRequested)
            {
                return;
            }

            // schedule streaming work if nothing is running
            if (_work is null)
            {
                _work = Task.Run(ExecuteLongAsync, _cancellation.Token);
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
        private async Task ExecuteLongAsync()
        {
            try
            {
                // this helps cancel every local step upon stream failure at any point
                using var localCancellation = new CancellationTokenSource();
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(localCancellation.Token, _cancellation.Token);

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

                var result = await _trader.Get24hTickerPriceChangeStatisticsAsync(symbol, cancellationToken);
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

            foreach (var item in _klineWindows)
            {
                // define the required window
                var end = _clock.UtcNow;
                var start = end.Subtract(item.Value);

                _logger.LogInformation(
                    "{Name} is syncing klines for {Symbol} from {Start} to {End}",
                    TypeName, item.Key.Symbol, start, end);

                var current = start;
                var count = 0;

                while (current < end)
                {
                    var klines = await _trader
                        .WithWaitOnTooManyRequests((t, ct) => t
                        .GetKlinesAsync(new GetKlines(item.Key.Symbol, item.Key.Interval, current, end, 1000), ct), _logger, cancellationToken);

                    if (klines.Count is 0) break;

                    foreach (var kline in klines)
                    {
                        SetKline(kline);
                    }

                    current = klines.Max(x => x.OpenTime).AddMilliseconds(1);
                    count += klines.Count;

                    _logger.LogInformation(
                        "{Name} paged {Count} klines totalling {Total} klines for {Symbol}",
                        TypeName, klines.Count, count, item.Key.Symbol);
                }

                _logger.LogInformation(
                    "{Name} synced {Total} klines for {Symbol}",
                    TypeName, count, item.Key.Symbol);
            }

            _logger.LogInformation("{Name} synced klines for {Symbols} in {ElapsedMs}ms...", TypeName, _klineWindows.Select(x => x.Key.Symbol), watch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Saves conflated tickers to the repository.
        /// </summary>
        private async Task TickSaveTickersAsync()
        {
            var tickerBuffer = ArrayPool<MiniTicker>.Shared.Rent(_tickerSymbols.Count);
            var versionBuffer = ArrayPool<Guid>.Shared.Rent(_tickerSymbols.Count);
            var count = 0;

            // stage the unsaved tickers from the conflation
            foreach (var item in _tickers.Select(x => x.Value))
            {
                tickerBuffer[count] = item.Ticker;
                versionBuffer[count] = item.Version;
                count++;
            }

            // save all tickers in one go
            if (count > 0)
            {
                await _repository.SetTickersAsync(tickerBuffer.Take(count), _cancellation.Token);

                // mark unchanged saved tickers in the concurrent conflation to avoid saving them again
                for (var i = 0; i < count; i++)
                {
                    var ticker = tickerBuffer[i];
                    var version = versionBuffer[i];

                    // the concurrent dictionary may have changed in the background so we must avoid marking newer data as saved
                    _tickers.TryUpdate(ticker.Symbol, (ticker, version, true), (ticker, version, false));
                }
            }

            ArrayPool<MiniTicker>.Shared.Return(tickerBuffer);
            ArrayPool<Guid>.Shared.Return(versionBuffer);
        }

        /// <summary>
        /// Saves conflated klines to the repository.
        /// </summary>
        private async Task TickSaveKlinesAsync()
        {
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
                await _repository.SetKlinesAsync(buffer.Take(count), _cancellation.Token);

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
        private Task TickClearKlinesAsync()
        {
            var buffer = ArrayPool<Kline>.Shared.Rent(_klines.Count);
            var count = 0;

            // elect expired saved klines for removal
            var now = _clock.UtcNow;
            foreach (var item in _klines)
            {
                // protect against concurrent dictionary size increasing during enumeration
                if (count >= buffer.Length) break;

                // attempt to elect the item for removal
                if (_klineWindows.TryGetValue((item.Key.Symbol, item.Key.Interval), out var window) && item.Key.OpenTime < now.Subtract(window) && item.Value.Saved)
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

        // todo: refactor this into a local replica grain
        public ValueTask<IReadOnlyList<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime start, DateTime end)
        {
            var builder = ImmutableSortedSet.CreateBuilder(Kline.OpenTimeComparer);

            foreach (var time in interval.Range(start, end))
            {
                if (!_klines.TryGetValue((symbol, interval, time), out var value))
                {
                    throw new KeyNotFoundException($"Could not provide kline for (Symbol = '{symbol}', Interval = '{interval}', OpenTime = '{time}')");
                }

                builder.Add(value.Kline);
            }

            return ValueTask.FromResult<IReadOnlyList<Kline>>(builder.ToImmutable());
        }

        public Task<bool> IsReadyAsync() => Task.FromResult(_ready);

        #region Kline Long Polling

        private void SetKline(Kline kline)
        {
            // keep the kline if brand new or newer than existing one
            var candidate = (kline, false);
            _klines.AddOrUpdate(
                (kline.Symbol, kline.Interval, kline.OpenTime),
                (key, arg) => arg,
                (key, current, arg) => arg.kline.EventTime > current.Kline.EventTime ? arg : current,
                candidate);
        }

        #endregion Kline Long Polling

        #region Ticker Long Polling

        private void SetTicker(MiniTicker ticker)
        {
            // compose the dictionary candidate
            var version = Guid.NewGuid();
            var candidate = (ticker, version, false);

            // keep the ticker if brand new or newer than existing one
            var result = _tickers.AddOrUpdate(
                ticker.Symbol,
                (key, arg) => arg,
                (key, current, arg) => arg.ticker.EventTime > current.Ticker.EventTime ? arg : current,
                candidate);

            // publish the ticker to active long polling clients
            if (candidate == result && _tickerCompletions.Remove(ticker.Symbol, out var completion))
            {
                completion.SetResult((ticker, version));
            }
        }

        public ValueTask<(MiniTicker?, Guid)> LongPollTickerAsync(string symbol, Guid version)
        {
            // ensure a promise for the next version exists before checking the current version
            // this avoids concurrency issues with the ticker stream which could cause the long poll to miss updates
            var completion = _tickerCompletions.GetOrAdd(symbol, key => new TaskCompletionSource<(MiniTicker?, Guid)>(TaskCreationOptions.RunContinuationsAsynchronously));

            // check the current version
            if (_tickers.TryGetValue(symbol, out var item) && item.Version != version)
            {
                // the current item version is different from the client version so return the new version now
                return new ValueTask<(MiniTicker?, Guid)>((item.Ticker, item.Version));
            }

            // let the client wait for the new version to resolve or timeout
            return new ValueTask<(MiniTicker?, Guid)>(completion.Task.WithDefaultOnTimeout((null, Guid.Empty), _options.ReactivePollingDelay));
        }

        #endregion Ticker Long Polling
    }
}