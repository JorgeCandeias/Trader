using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Core.Timers;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Streams.MarketData
{
    internal class BinanceMarketDataGrain : Grain, IBinanceMarketDataGrain
    {
        private readonly BinanceOptions _options;
        private readonly ILogger _logger;
        private readonly IMarketDataStreamClientFactory _factory;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly IMapper _mapper;
        private readonly ISafeTimerFactory _timers;

        private readonly ISet<string> _tickerSymbols;

        public BinanceMarketDataGrain(ILogger<BinanceMarketDataGrain> logger, IOptions<BinanceOptions> options, IMarketDataStreamClientFactory factory, ITradingRepository repository, ITradingService trader, IMapper mapper, ISafeTimerFactory timers, IAlgoDependencyInfo dependencies)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _timers = timers ?? throw new ArgumentNullException(nameof(timers));

            _ = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _tickerSymbols = dependencies.GetTickers().ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => EnsureWorkAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            if (_tickerSymbols.Count > 0)
            {
                RegisterTimer(_ => SaveTickersAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
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

            // todo: add klines
            //streams.AddRange(_options.MarketDataStreamSymbols.Select(x => $"{x.ToLowerInvariant()}@kline_{_mapper.Map<string>(KlineInterval.Minutes1)}"));

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

                    // todo: add klines
                    /*
                    if (message.Kline is not null && _options.MarketDataStreamSymbols.Contains(message.Kline.Symbol))
                    {
                        await _klines.Writer.WriteAsync(message.Kline, cancellationToken).ConfigureAwait(false);
                    }
                    */
                }
            }, _cancellation.Token);

            // sync tickers from the api
            await SyncTickersAsync();

            // sync klines from the api
            //await SyncKlinesAsync(cancellationToken).ConfigureAwait(false);

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
        }

        public Task<MiniTicker?> TryGetTickerAsync(string symbol)
        {
            return Task.FromResult(_tickers.TryGetValue(symbol, out var value) ? value.Ticker : null);
        }
    }
}