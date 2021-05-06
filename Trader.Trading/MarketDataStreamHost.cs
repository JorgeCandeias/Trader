using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Core.Timers;
using Trader.Data;
using Trader.Models;
using Trader.Trading.Binance;

namespace Trader.Trading
{
    internal sealed class MarketDataStreamHost : IHostedService, IDisposable
    {
        private readonly MarketDataStreamHostOptions _options;
        private readonly ILogger _logger;
        private readonly IMarketDataStreamClientFactory _factory;
        private readonly ITraderRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly IMapper _mapper;
        private readonly ISafeTimerFactory _timers;

        public MarketDataStreamHost(IOptions<MarketDataStreamHostOptions> options, ILogger<MarketDataStreamHost> logger, IMarketDataStreamClientFactory factory, ITraderRepository repository, ITradingService trader, ISystemClock clock, IMapper mapper, ISafeTimerFactory timers)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _timers = timers ?? throw new ArgumentNullException(nameof(timers));
        }

        private static string Name => nameof(MarketDataStreamHost);

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

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "DTO")]
        private async Task TickWorkerAsync(CancellationToken cancellationToken)
        {
            // cancel the ready state if startup is cancelled
            using var registration = cancellationToken.Register(() => _ready.TrySetCanceled(cancellationToken));

            // create a client for the streams we want
            var streams = _options.Symbols.Select(x => $"{x.ToLowerInvariant()}@miniTicker").ToList();

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
                }
            }, cancellationToken);

            // wait for a few seconds for the stream to stabilize so we don't miss any incoming data from binance
            _logger.LogInformation("{Name} waiting {Period} for stream to stabilize...", Name, _options.StabilizationPeriod);
            await Task.Delay(_options.StabilizationPeriod, cancellationToken).ConfigureAwait(false);

            // sync tickers from the api
            await SyncTickersAsync(cancellationToken).ConfigureAwait(false);

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

        private async Task TickSaveAsync(CancellationToken cancellationToken)
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

                //_logger.LogInformation("{Name} saved prices for {Symbols}", Name, segment.Select(x => x.Symbol));
            }

            ArrayPool<MiniTicker>.Shared.Return(buffer);
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

        ~MarketDataStreamHost()
        {
            Dispose(false);
        }

        #endregion Disposable
    }
}