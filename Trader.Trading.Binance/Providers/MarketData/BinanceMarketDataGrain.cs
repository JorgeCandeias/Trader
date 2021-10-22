using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class BinanceMarketDataGrain : Grain, IBinanceMarketDataGrain
    {
        private readonly ILogger _logger;
        private readonly IMarketDataStreamClientFactory _factory;
        private readonly ITradingService _trader;
        private readonly IMapper _mapper;
        private readonly ISystemClock _clock;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IKlineProvider _klines;
        private readonly ITickerProvider _tickers;

        private readonly HashSet<string> _tickerSymbols;
        private readonly Dictionary<(string Symbol, KlineInterval Interval), int> _klineWindows;

        public BinanceMarketDataGrain(ILogger<BinanceMarketDataGrain> logger, IMarketDataStreamClientFactory factory, ITradingService trader, IMapper mapper, ISystemClock clock, IAlgoDependencyInfo dependencies, IHostApplicationLifetime lifetime, IKlineProvider provider, ITickerProvider tickers)
        {
            _logger = logger;
            _factory = factory;
            _trader = trader;
            _mapper = mapper;
            _clock = clock;
            _lifetime = lifetime;
            _klines = provider;
            _tickers = tickers;

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

            return base.OnActivateAsync();
        }

        public Task<bool> IsReadyAsync() => Task.FromResult(_ready);

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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Worker Task")]
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
                    // this worker action pushes incoming klines to the system in the background so we dont hold up the binance stream
                    var klineWorker = new ActionBlock<Kline>(async item =>
                    {
                        try
                        {
                            await _klines.SetKlineAsync(item, linkedCancellation.Token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Name} failed to push kline {Kline}", TypeName, item);
                        }
                    }, new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = _klineWindows.Count
                    });

                    // this worker action pushes incoming tickers to the system in the background so we dont hold up the binance stream
                    var tickerWorker = new ActionBlock<MiniTicker>(async item =>
                    {
                        try
                        {
                            await _tickers.SetTickerAsync(item, _lifetime.ApplicationStopping);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "{Name} failed to push ticker {Ticker}", TypeName, item);
                        }
                    }, new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = _tickerSymbols.Count
                    });

                    while (!linkedCancellation.Token.IsCancellationRequested)
                    {
                        var message = await client.ReceiveAsync(linkedCancellation.Token);

                        if (message.Error is not null)
                        {
                            throw new BinanceCodeException(message.Error.Code, message.Error.Message, 0);
                        }

                        if (message.MiniTicker is not null && _tickerSymbols.Contains(message.MiniTicker.Symbol))
                        {
                            tickerWorker.Post(message.MiniTicker);
                        }

                        if (message.Kline is not null && _klineWindows.ContainsKey((message.Kline.Symbol, message.Kline.Interval)))
                        {
                            klineWorker.Post(message.Kline);
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
        /// Syncs tickers from binance into the system.
        /// </summary>
        private async Task SyncTickersAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is syncing tickers for {Symbols}...", TypeName, _tickerSymbols);
            var watch = Stopwatch.StartNew();

            // batches saving work in the background so we can keep pulling data without waiting
            var work = new ActionBlock<MiniTicker>(item => _tickers.SetTickerAsync(item, _lifetime.ApplicationStopping));

            foreach (var symbol in _tickerSymbols)
            {
                var subWatch = Stopwatch.StartNew();

                var result = await _trader
                    .WithBackoff()
                    .Get24hTickerPriceChangeStatisticsAsync(symbol, cancellationToken);

                var ticker = _mapper.Map<MiniTicker>(result);

                work.Post(ticker);

                _logger.LogInformation("{Name} synced ticker for {Symbol} in {ElapsedMs}ms", TypeName, symbol, subWatch.ElapsedMilliseconds);
            }

            work.Complete();
            await work.Completion;

            _logger.LogInformation("{Name} synced tickers for {Symbols} in {ElapsedMs}ms", TypeName, _tickerSymbols, watch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Syncs klines from binance into the system.
        /// </summary>
        private async Task SyncKlinesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Name} is syncing klines for {Symbols}...", TypeName, _klineWindows.Select(x => x.Key.Symbol));
            var watch = Stopwatch.StartNew();

            var end = _clock.UtcNow;

            // batches saving work in the background so we can keep pulling data without waiting
            var work = new ActionBlock<(string Symbol, KlineInterval Interval, IEnumerable<Kline> Items)>(item => _klines.SetKlinesAsync(item.Symbol, item.Interval, item.Items));

            // pull everything now
            foreach (var item in _klineWindows)
            {
                // define the required window
                var start = end.Subtract(item.Key.Interval, item.Value).AdjustToNext(item.Key.Interval);

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

                    // queue the page for saving
                    work.Post((item.Key.Symbol, item.Key.Interval, klines));

                    _logger.LogInformation(
                        "{Name} paged {Count} klines for {Symbol} {Interval} between {Start} and {End} for a total of {Total} klines",
                        TypeName, klines.Count, item.Key.Symbol, item.Key.Interval, current, end, total);

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

            _logger.LogInformation("{Name} synced klines for {Symbols} in {ElapsedMs}ms...", TypeName, _klineWindows.Select(x => x.Key.Symbol), watch.ElapsedMilliseconds);
        }
    }
}