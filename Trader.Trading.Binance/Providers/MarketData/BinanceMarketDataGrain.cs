using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Timers;
using Outcompute.Trader.Trading.Algorithms;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal partial class BinanceMarketDataGrain : Grain, IBinanceMarketDataGrain
    {
        private readonly IOptionsMonitor<BinanceOptions> _options;
        private readonly IAlgoDependencyResolver _dependencies;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ITimerRegistry _timers;
        private readonly ITickerSynchronizer _tickerSynchronizer;
        private readonly IKlineSynchronizer _klineSynchronizer;
        private readonly IMarketDataStreamer _streamer;

        public BinanceMarketDataGrain(IOptionsMonitor<BinanceOptions> options, IAlgoDependencyResolver dependencies, ILogger<BinanceMarketDataGrain> logger, IHostApplicationLifetime lifetime, ITimerRegistry timers, ITickerSynchronizer tickerSynchronizer, IKlineSynchronizer klineSynchronizer, IMarketDataStreamer streamer)
        {
            _options = options;
            _dependencies = dependencies;
            _logger = logger;
            _lifetime = lifetime;
            _timers = timers;
            _tickerSynchronizer = tickerSynchronizer;
            _klineSynchronizer = klineSynchronizer;
            _streamer = streamer;
        }

        private const string TypeName = nameof(BinanceMarketDataGrain);

        private Task? _work;

        private bool _ready;

        private IDisposable? _timer;

        public override Task OnActivateAsync()
        {
            // if there are ticker or kline dependencies then ensure we keep streaming them
            if (_dependencies.Symbols.Count + _dependencies.Tickers.Count + _dependencies.Balances.Count + _dependencies.Klines.Count > 0)
            {
                _timer = _timers.RegisterTimer(this, TickEnsureStreamAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

            LogStarted(TypeName);

            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _timer?.Dispose();

            LogStopped(TypeName);

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
            var options = _options.CurrentValue;

            // this helps cancel every local step upon stream failure at any point
            using var resetCancellation = new CancellationTokenSource(options.MarketDataStreamResetPeriod);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(resetCancellation.Token, _lifetime.ApplicationStopping);

            // start streaming in the background while we sync from the api
            var streamTask = Task.Run(() => _streamer.StreamAsync(_dependencies.AllSymbols, _dependencies.Klines.Keys, linkedCancellation.Token), linkedCancellation.Token);

            // sync tickers from the api
            await _tickerSynchronizer.SyncAsync(_dependencies.AllSymbols, linkedCancellation.Token);

            // sync klines from the api
            await _klineSynchronizer.SyncAsync(_dependencies.Klines.Select(x => (x.Key.Symbol, x.Key.Interval, x.Value)), linkedCancellation.Token);

            // signal the ready state to allow algos to execute
            _ready = true;

            // keep streaming now
            await streamTask;
        }

        public Task PingAsync() => Task.CompletedTask;

        #region Logging

        [LoggerMessage(0, LogLevel.Information, "{Type} started")]
        private partial void LogStarted(string type);

        [LoggerMessage(1, LogLevel.Information, "{Type} stopped")]
        private partial void LogStopped(string type);

        #endregion Logging
    }
}