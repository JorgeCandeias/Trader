using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal class BinanceMarketDataGrain : Grain, IBinanceMarketDataGrain
    {
        private readonly BinanceOptions _options;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ITimerRegistry _timers;
        private readonly ITickerSynchronizer _tickerSynchronizer;
        private readonly IKlineSynchronizer _klineSynchronizer;
        private readonly IMarketDataStreamer _streamer;

        private readonly HashSet<string> _tickerSymbols;
        private readonly Dictionary<(string Symbol, KlineInterval Interval), int> _klineWindows;

        public BinanceMarketDataGrain(IOptions<BinanceOptions> options, ILogger<BinanceMarketDataGrain> logger, IAlgoDependencyInfo dependencies, IHostApplicationLifetime lifetime, ITimerRegistry timers, ITickerSynchronizer tickerSynchronizer, IKlineSynchronizer klineSynchronizer, IMarketDataStreamer streamer)
        {
            _options = options.Value;
            _logger = logger;
            _lifetime = lifetime;
            _timers = timers;
            _tickerSynchronizer = tickerSynchronizer;
            _klineSynchronizer = klineSynchronizer;
            _streamer = streamer;

            _tickerSymbols = dependencies.GetTickers().ToHashSet(StringComparer.OrdinalIgnoreCase);

            _klineWindows = dependencies
                .GetKlines()
                .GroupBy(x => (x.Symbol, x.Interval))
                .Select(x => (x.Key, Periods: x.Max(y => y.Periods)))
                .ToDictionary(x => x.Key, x => x.Periods);
        }

        private static string TypeName => nameof(BinanceMarketDataGrain);

        private Task? _work;

        private bool _ready;

        private IDisposable? _timer;

        public override Task OnActivateAsync()
        {
            // if there are ticker or kline dependencies then ensure we keep streaming them
            if (_tickerSymbols.Count > 0 || _klineWindows.Count > 0)
            {
                _timer = _timers.RegisterTimer(this, TickEnsureStreamAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            _logger.LogInformation("{Name} started", TypeName);

            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _timer?.Dispose();

            _logger.LogInformation("{Name} stopped", TypeName);

            return base.OnDeactivateAsync();
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

        private async Task ExecuteStreamAsync()
        {
            try
            {
                // this helps cancel every local step upon stream failure at any point
                using var resetCancellation = new CancellationTokenSource(_options.MarketDataStreamResetPeriod);
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(resetCancellation.Token, _lifetime.ApplicationStopping);

                // start streaming in the background while we sync from the api
                var streamTask = Task.Run(() => _streamer.StreamAsync(_tickerSymbols, _klineWindows.Select(x => (x.Key.Symbol, x.Key.Interval)).ToHashSet(), linkedCancellation.Token), linkedCancellation.Token);

                // sync tickers from the api
                await _tickerSynchronizer.SyncAsync(_tickerSymbols, linkedCancellation.Token);

                // sync klines from the api
                await _klineSynchronizer.SyncAsync(_klineWindows.Select(x => (x.Key.Symbol, x.Key.Interval, x.Value)), linkedCancellation.Token);

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

        public Task PingAsync() => Task.CompletedTask;
    }
}