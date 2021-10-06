using AutoMapper;
using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Polly;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Streams.MarketData
{
    internal class BinanceMarketDataGrain : Grain, IBinanceMarketDataGrain
    {
        private readonly ILogger _logger;
        private readonly IMarketDataStreamClientFactory _factory;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly IMapper _mapper;
        private readonly ISystemClock _clock;

        private readonly HashSet<string> _tickerSymbols;
        private readonly Dictionary<(string Symbol, KlineInterval Interval), TimeSpan> _klineItems;

        public BinanceMarketDataGrain(ILogger<BinanceMarketDataGrain> logger, IMarketDataStreamClientFactory factory, ITradingRepository repository, ITradingService trader, IMapper mapper, ISystemClock clock, IAlgoDependencyInfo dependencies)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _tickerSymbols = dependencies.GetTickers().ToHashSet(StringComparer.OrdinalIgnoreCase);

            _klineItems = dependencies
                .GetKlines()
                .GroupBy(x => (x.Symbol, x.Interval))
                .Select(x => (x.Key, Window: x.Max(y => y.Window)))
                .ToDictionary(x => x.Key, x => x.Window);
        }

        private static string TypeName => nameof(BinanceMarketDataGrain);

        private readonly CancellationTokenSource _cancellation = new();

        /// <summary>
        /// Holds the background streaming and syncing work.
        /// </summary>
        private Task? _work = null;

        /// <summary>
        /// Conflates the incoming tickers from the background stream so the repository has a chance to keep up.
        /// </summary>
        private readonly ConcurrentDictionary<string, (MiniTicker Ticker, bool Saved)> _tickers = new();

        /// <summary>
        /// Conflates the incoming klines from the background stream so the repository has a chance to keep up.
        /// </summary>
        private readonly ConcurrentDictionary<(string Symbol, KlineInterval Interval, DateTime OpenTime), (Kline Kline, bool Saved)> _klines = new();

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => EnsureWorkAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            if (_tickerSymbols.Count > 0)
            {
                RegisterTimer(_ => SaveTickersAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            if (_klineItems.Count > 0)
            {
                RegisterTimer(_ => SaveKlinesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                RegisterTimer(_ => ClearKlinesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Dispose();

            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// Monitors the background streaming work task and ensures it remains active upon faulting.
        /// </summary>
        private async Task EnsureWorkAsync()
        {
            // avoid starting streaming work upon shutdown
            if (_cancellation.IsCancellationRequested)
            {
                return;
            }

            // schedule streaming work if nothing is running
            if (_work is null)
            {
                _work = Task.Run(() => ExecuteAsync(), _cancellation.Token);
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

        private async Task ExecuteAsync()
        {
            // create a client for the streams we want
            var streams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            streams.UnionWith(_tickerSymbols.Select(x => $"{x.ToLowerInvariant()}@miniTicker"));
            streams.UnionWith(_klineItems.Select(x => $"{x.Key.Symbol.ToLowerInvariant()}@kline_{_mapper.Map<string>(x.Key.Interval)}"));

            _logger.LogInformation("{Name} connecting to streams {Streams}...", TypeName, streams);

            using var client = _factory.Create(streams);

            await client.ConnectAsync(_cancellation.Token);

            // start streaming in the background while we sync from the api
            var streamTask = Task.Run(async () =>
            {
                while (!_cancellation.Token.IsCancellationRequested)
                {
                    var message = await client.ReceiveAsync(_cancellation.Token);

                    if (message.Error is not null)
                    {
                        throw new BinanceCodeException(message.Error.Code, message.Error.Message, 0);
                    }

                    if (message.MiniTicker is not null && _tickerSymbols.Contains(message.MiniTicker.Symbol))
                    {
                        // conflate the ticker if it is newer than the cached one - otherwise discard it
                        _tickers.AddOrUpdate(message.MiniTicker.Symbol, (k, arg) => arg, (k, e, arg) => arg.Ticker.EventTime > e.Ticker.EventTime ? arg : e, (Ticker: message.MiniTicker, Saved: false));
                    }

                    if (message.Kline is not null && _klineItems.ContainsKey((message.Kline.Symbol, message.Kline.Interval)))
                    {
                        // conflate the kline if it is newer than the cached one - otherwise discard it
                        _klines.AddOrUpdate((message.Kline.Symbol, message.Kline.Interval, message.Kline.OpenTime), (k, arg) => arg, (k, e, arg) => arg.Kline.EventTime > e.Kline.EventTime ? arg : e, (message.Kline, Saved: false));
                    }
                }
            }, _cancellation.Token);

            // sync tickers from the api
            await SyncTickersAsync().ConfigureAwait(false);

            // sync klines from the api
            await SyncKlinesAsync().ConfigureAwait(false);

            // keep streaming now
            await streamTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Syncs tickers from binance into the conflation cache.
        /// </summary>
        private async Task SyncTickersAsync()
        {
            _logger.LogInformation("{Name} is syncing tickers for {Symbols}...", TypeName, _tickerSymbols);
            var watch = Stopwatch.StartNew();

            foreach (var symbol in _tickerSymbols)
            {
                var result = await _trader.Get24hTickerPriceChangeStatisticsAsync(symbol, _cancellation.Token);
                var ticker = _mapper.Map<MiniTicker>(result);
                var value = (Ticker: ticker, Saved: false);

                // conflate the ticker if it is newer than the cached one - otherwise discard it
                _tickers.AddOrUpdate(symbol, (k, arg) => arg, (k, e, arg) => arg.Ticker.EventTime > e.Ticker.EventTime ? arg : e, value);
            }

            _logger.LogInformation("{Name} synced tickers for {Symbols} in {ElapsedMs}ms", TypeName, _tickerSymbols, watch.ElapsedMilliseconds);
        }

        private async Task SyncKlinesAsync()
        {
            foreach (var item in _klineItems)
            {
                var start = DateTime.UtcNow.Subtract(item.Value);
                var end = DateTime.UtcNow;

                _logger.LogInformation(
                    "{Name} is syncing klines for {Symbol} from {Start} to {End}",
                    TypeName, item.Key.Symbol, start, end);

                var current = start;
                var count = 0;

                while (current < end)
                {
                    var klines = await Policy
                        .Handle<BinanceTooManyRequestsException>()
                        .WaitAndRetryForeverAsync(
                            (n, ex, ctx) => ((BinanceTooManyRequestsException)ex).RetryAfter,
                            (ex, ts, ctx) =>
                            {
                                _logger.LogWarning(ex, "{Name} backing off for {TimeSpan}...", TypeName, ts);

                                return Task.CompletedTask;
                            })
                        .ExecuteAsync(ct => _trader.GetKlinesAsync(new GetKlines(item.Key.Symbol, item.Key.Interval, current, end, 1000), ct), _cancellation.Token, false)
                        .ConfigureAwait(false);

                    if (klines.Count is 0) break;

                    foreach (var kline in klines)
                    {
                        _klines.AddOrUpdate((kline.Symbol, kline.Interval, kline.OpenTime), (k, arg) => arg, (k, e, arg) => arg.Kline.EventTime > e.Kline.EventTime ? arg : e, (Kline: kline, Saved: false));
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
        }

        /// <summary>
        /// Saves conflated tickers to the repository.
        /// </summary>
        private async Task SaveTickersAsync()
        {
            var buffer = ArrayPool<MiniTicker>.Shared.Rent(_tickerSymbols.Count);
            var count = 0;

            // stage the unsaved tickers from the concurrent conflation
            foreach (var symbol in _tickerSymbols)
            {
                if (_tickers.TryGetValue(symbol, out var value) && !value.Saved)
                {
                    buffer[count++] = value.Ticker;
                }
            }

            // save all tickers in one go
            if (count > 0)
            {
                var segment = new ArraySegment<MiniTicker>(buffer, 0, count);

                await _repository.SetTickersAsync(segment, _cancellation.Token);

                // mark unchanged saved tickers in the concurrent conflation to avoid saving them again
                foreach (var ticker in segment)
                {
                    _tickers.TryUpdate(ticker.Symbol, (ticker, true), (ticker, false));
                }
            }

            ArrayPool<MiniTicker>.Shared.Return(buffer);
        }

        /// <summary>
        /// Saves conflated klines to the repository.
        /// </summary>
        private async Task SaveKlinesAsync()
        {
            var buffer = ArrayPool<Kline>.Shared.Rent(_klines.Count);
            var count = 0;

            // stage the unsaved klines from the concurrent conflation
            foreach (var item in _klines)
            {
                // break if the buffer is full
                if (count >= buffer.Length) break;

                // skip saved items
                if (item.Value.Saved) continue;

                // stage unsaved items
                buffer[count++] = item.Value.Kline;
            }

            // save all klines to the repository
            if (count > 0)
            {
                var segment = new ArraySegment<Kline>(buffer, 0, count);

                await _repository.SetKlinesAsync(segment, _cancellation.Token).ConfigureAwait(false);

                // mark unsaved klines as saved to avoid saving them again
                foreach (var kline in segment)
                {
                    _klines.TryUpdate((kline.Symbol, kline.Interval, kline.OpenTime), (kline, true), (kline, false));
                }
            }

            ArrayPool<Kline>.Shared.Return(buffer);
        }

        /// <summary>
        /// Clears old klines from memory.
        /// </summary>
        private Task ClearKlinesAsync()
        {
            var buffer = ArrayPool<Kline>.Shared.Rent(_klines.Count);
            var count = 0;

            // elect klines for removal
            // we only elected old klines which have not been saved yet
            var now = _clock.UtcNow;
            foreach (var item in _klines)
            {
                if (_klineItems.TryGetValue((item.Key.Symbol, item.Key.Interval), out var window) && item.Key.OpenTime < now.Subtract(window) && item.Value.Saved)
                {
                    buffer[count++] = item.Value.Kline;
                }
            }

            // safely remove the elected items
            for (var i = 0; i < count; i++)
            {
                var item = buffer[i];

                _klines.TryRemove(new KeyValuePair<(string Symbol, KlineInterval Interval, DateTime OpenTime), (Kline Kline, bool Saved)>((item.Symbol, item.Interval, item.OpenTime), (item, true)));
            }

            ArrayPool<Kline>.Shared.Return(buffer);

            return Task.CompletedTask;
        }

        // todo: refactor this into a local replica grain
        public Task<MiniTicker?> TryGetTickerAsync(string symbol)
        {
            return Task.FromResult(_tickers.TryGetValue(symbol, out var value) ? value.Ticker : null);
        }

        // todo: refactor this into a local replica grain
        public Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime start, DateTime end)
        {
            var builder = ImmutableList.CreateBuilder<Kline>();

            foreach (var item in _klines)
            {
                if (item.Key.Symbol == symbol && item.Key.Interval == interval && item.Key.OpenTime <= start && item.Key.OpenTime >= end)
                {
                    builder.Add(item.Value.Kline);
                }
            }

            return Task.FromResult<IEnumerable<Kline>>(builder.ToImmutable());
        }
    }
}