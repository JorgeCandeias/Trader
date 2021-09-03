using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Outcompute.Trader.Core.Timers;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Streams.Market
{
    internal sealed class BinanceMarketDataStreamHost : IHostedService, IDisposable
    {
        private readonly BinanceMarketDataStreamHostOptions _options;
        private readonly ILogger _logger;
        private readonly IMarketDataStreamClientFactory _factory;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly IMapper _mapper;
        private readonly ISafeTimerFactory _timers;

        public BinanceMarketDataStreamHost(IOptions<BinanceMarketDataStreamHostOptions> options, ILogger<BinanceMarketDataStreamHost> logger, IMarketDataStreamClientFactory factory, ITradingRepository repository, ITradingService trader, IMapper mapper, ISafeTimerFactory timers)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _timers = timers ?? throw new ArgumentNullException(nameof(timers));
        }

        private static string Name => nameof(BinanceMarketDataStreamHost);

        /// <summary>
        /// Cancels the worker task.
        /// </summary>
        private readonly CancellationTokenSource _cancellation = new();

        /// <summary>
        /// Holds the combined startup cancellation so it doesn't get garbage collected.
        /// </summary>
        private CancellationTokenSource? _linkedStartupCancellation;

        /// <summary>
        /// Holds the resilient work timer.
        /// </summary>
        private ISafeTimer? _workTimer;

        /// <summary>
        /// Holds the resilient save timer.
        /// </summary>
        private ISafeTimer? _saveTimer;

        /// <summary>
        /// Signals that the first sync sequence has completed.
        /// </summary>
        private readonly TaskCompletionSource _ready = new();

        /// <summary>
        /// Conflates the incoming tickers so the repository has a chance to keep up.
        /// </summary>
        private readonly ConcurrentDictionary<string, MiniTicker> _tickers = new();

        /// <summary>
        /// Conflates the incoming kline data.
        /// </summary>
        private readonly KlineConflatorChannel _klines = new();

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "DTO")]
        private async Task TickWorkerAsync(CancellationToken cancellationToken)
        {
            // cancel the ready state if startup is cancelled
            using var registration = cancellationToken.Register(() => _ready.TrySetCanceled(cancellationToken));

            // create a client for the streams we want
            var streams = new List<string>();
            streams.AddRange(_options.Symbols.Select(x => $"{x.ToLowerInvariant()}@miniTicker"));
            streams.AddRange(_options.Symbols.Select(x => $"{x.ToLowerInvariant()}@kline_{_mapper.Map<string>(KlineInterval.Minutes1)}"));

            _logger.LogInformation("{Name} connecting to streams {Streams}...", Name, streams);

            using var client = _factory.Create(streams);

            await client
                .ConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            // start streaming in the background while we sync from the api
            var streamTask = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = await client
                        .ReceiveAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (message.Error is not null)
                    {
                        throw new BinanceCodeException(message.Error.Code, message.Error.Message, 0);
                    }

                    if (message.MiniTicker is not null && _options.Symbols.Contains(message.MiniTicker.Symbol))
                    {
                        _tickers[message.MiniTicker.Symbol] = message.MiniTicker;
                    }

                    if (message.Kline is not null && _options.Symbols.Contains(message.Kline.Symbol))
                    {
                        await _klines.Writer.WriteAsync(message.Kline, cancellationToken).ConfigureAwait(false);
                    }
                }
            }, cancellationToken);

            // wait for a few seconds for the stream to stabilize so we don't miss any incoming data from binance
            _logger.LogInformation("{Name} waiting {Period} for stream to stabilize...", Name, _options.StabilizationPeriod);
            await Task.Delay(_options.StabilizationPeriod, cancellationToken).ConfigureAwait(false);

            // sync tickers from the api
            await SyncTickersAsync(cancellationToken).ConfigureAwait(false);

            // sync klines from the api
            await SyncKlinesAsync(cancellationToken).ConfigureAwait(false);

            // signal the start method that everything is ready
            _ready.TrySetResult();

            // keep streaming now
            await streamTask.ConfigureAwait(false);
        }

        private async Task SyncTickersAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is syncing tickers for {Symbols}...", Name, _options.Symbols);

            var buffer = ArrayPool<MiniTicker>.Shared.Rent(_options.Symbols.Count);
            var segment = new ArraySegment<MiniTicker>(buffer, 0, _options.Symbols.Count);
            var count = 0;

            foreach (var symbol in _options.Symbols)
            {
                var ticker = await _trader
                    .Get24hTickerPriceChangeStatisticsAsync(symbol, cancellationToken)
                    .ConfigureAwait(false);

                segment[count++] = _mapper.Map<MiniTicker>(ticker);
            }

            await _repository
                .SetTickersAsync(segment, cancellationToken)
                .ConfigureAwait(false);

            ArrayPool<MiniTicker>.Shared.Return(buffer);

            _logger.LogInformation("{Name} synced tickers for {Tickers}", Name, segment.Select(x => x.Symbol));
        }

        private async Task SyncKlinesAsync(CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow.Subtract(_options.KlineHistorySpan);
            var end = DateTime.UtcNow;

            _logger.LogInformation("{Name} is syncing klines for {Symbols} from {Start} to {End}", Name, _options.Symbols, start, end);

            foreach (var symbol in _options.Symbols)
            {
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
                                _logger.LogWarning(ex,
                                    "{Name} backing off for {TimeSpan}...",
                                    Name, ts);

                                return Task.CompletedTask;
                            })
                        .ExecuteAsync(ct => _trader.GetKlinesAsync(new GetKlines(symbol, KlineInterval.Minutes1, current, end, 1000), ct), cancellationToken, false)
                        .ConfigureAwait(false);

                    if (klines.Count is 0) break;

                    await _repository
                        .SetKlinesAsync(klines, cancellationToken)
                        .ConfigureAwait(false);

                    current = klines.Max(x => x.OpenTime).AddMilliseconds(1);
                    count += klines.Count;
                }

                _logger.LogInformation("{Name} synced {Count} kline items for {Symbol}", Name, count, symbol);
            }
        }

        private async Task TickSaveAsync(CancellationToken cancellationToken)
        {
            await SaveTickersAsync(cancellationToken).ConfigureAwait(false);
            await SaveKlinesAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task SaveTickersAsync(CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<MiniTicker>.Shared.Rent(_options.Symbols.Count);
            var count = 0;

            foreach (var symbol in _options.Symbols)
            {
                if (_tickers.TryRemove(symbol, out var ticker))
                {
                    buffer[count++] = ticker;
                }
            }

            if (count > 0)
            {
                var segment = new ArraySegment<MiniTicker>(buffer, 0, count);

                await _repository
                    .SetTickersAsync(segment, cancellationToken)
                    .ConfigureAwait(false);
            }

            ArrayPool<MiniTicker>.Shared.Return(buffer);
        }

        private async Task SaveKlinesAsync(CancellationToken cancellationToken)
        {
            if (_klines.Reader.TryRead(out var klines))
            {
                await _repository
                    .SetKlinesAsync(klines, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        #region Hosted Service

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} starting...", Name);

            // combine the startup cancellation so we can handle early process start cancellations properly
            _linkedStartupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellation.Token);

            // start the work timer
            _workTimer = _timers.Create(TickWorkerAsync, TimeSpan.Zero, _options.RetryDelay, Timeout.InfiniteTimeSpan);

            // start the save timer
            _saveTimer = _timers.Create(TickSaveAsync, TimeSpan.Zero, TimeSpan.FromMilliseconds(100), Timeout.InfiniteTimeSpan);

            // cancel everything early if requested
            using var workCancellation = cancellationToken.Register(() => _workTimer.Dispose());
            using var saveCancellation = cancellationToken.Register(() => _saveTimer.Dispose());
            using var readyCancellation = cancellationToken.Register(() => _ready.TrySetCanceled(cancellationToken));

            // wait for everything to sync before letting other services start
            await _ready.Task.ConfigureAwait(false);

            _logger.LogInformation("{Name} started", Name);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} stopping...", Name);

            // cancel any background work
            _cancellation.Dispose();
            _workTimer?.Dispose();
            _saveTimer?.Dispose();

            _logger.LogInformation("{Name} stopped", Name);

            return Task.CompletedTask;
        }

        #endregion Hosted Service

        #region Disposable

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _cancellation.Dispose();
                _workTimer?.Dispose();
                _saveTimer?.Dispose();
                _linkedStartupCancellation?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BinanceMarketDataStreamHost()
        {
            Dispose(false);
        }

        #endregion Disposable

        private class KlineConflatorChannel : Channel<Kline, IEnumerable<Kline>>
        {
            private readonly Dictionary<(string Symbol, KlineInterval Interval, DateTime OpenTime), Kline> _conflation = new();
            private readonly SemaphoreSlim _semaphore = new(1, 1);

            internal KlineConflatorChannel()
            {
                Writer = new KlineConflatorWriter(this);
                Reader = new KlineConflatorReader(this);
            }

            private ValueTask<bool> WaitToAccessAsync(CancellationToken cancellationToken = default)
            {
                // quick path for channel available
                if (_semaphore.CurrentCount > 0)
                {
                    return new ValueTask<bool>(true);
                }

                // slow path for awaiting
                return new ValueTask<bool>(WaitToAccessInnerAsync(cancellationToken));
            }

            private async Task<bool> WaitToAccessInnerAsync(CancellationToken cancellationToken)
            {
                // wait until the channel is free
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // release it as this is just a test
                _semaphore.Release();

                return true;
            }

            private class KlineConflatorReader : ChannelReader<IEnumerable<Kline>>
            {
                private readonly KlineConflatorChannel _channel;

                public KlineConflatorReader(KlineConflatorChannel channel)
                {
                    _channel = channel ?? throw new ArgumentNullException(nameof(channel));
                }

                public override bool TryRead([MaybeNullWhen(false)] out IEnumerable<Kline> item)
                {
                    // attempt to reserve the channel
                    if (!_channel._semaphore.Wait(TimeSpan.Zero))
                    {
                        item = null;
                        return false;
                    }

                    try
                    {
                        // quick path for no items in the conflation
                        if (_channel._conflation.Count is 0)
                        {
                            item = null;
                            return false;
                        }

                        // otherwise switch the conflation and return the items
                        item = _channel._conflation.Values.ToList();
                        _channel._conflation.Clear();
                        return true;
                    }
                    finally
                    {
                        // always release the channel
                        _channel._semaphore.Release();
                    }
                }

                public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
                {
                    return _channel.WaitToAccessAsync(cancellationToken);
                }
            }

            private class KlineConflatorWriter : ChannelWriter<Kline>
            {
                private readonly KlineConflatorChannel _channel;

                public KlineConflatorWriter(KlineConflatorChannel channel)
                {
                    _channel = channel ?? throw new ArgumentNullException(nameof(channel));
                }

                public override bool TryWrite(Kline item)
                {
                    if (item is null) throw new ArgumentNullException(nameof(item));

                    // attempt to reserve the channel without blocking
                    if (!_channel._semaphore.Wait(TimeSpan.Zero))
                    {
                        return false;
                    }

                    // conflate the item
                    var key = (item.Symbol, item.Interval, item.OpenTime);
                    if (!_channel._conflation.TryGetValue(key, out var current) || item.EventTime > current.EventTime)
                    {
                        _channel._conflation[key] = item;
                    }

                    // release the channel
                    _channel._semaphore.Release();
                    return true;
                }

                public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
                {
                    return _channel.WaitToAccessAsync(cancellationToken);
                }
            }
        }
    }
}