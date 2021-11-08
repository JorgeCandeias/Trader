using Orleans;
using Orleans.Concurrency;
using Orleans.Timers;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    /// <summary>
    /// Actively pulls the readyness state of the <see cref="BinanceMarketDataGrain"/> into each silo that needs it.
    /// </summary>
    [Reentrant]
    [StatelessWorker(1)]
    internal class BinanceMarketDataReadynessGrain : Grain, IBinanceMarketDataReadynessGrain
    {
        private readonly IGrainFactory _factory;
        private readonly ITimerRegistry _timers;

        public BinanceMarketDataReadynessGrain(IGrainFactory factory, ITimerRegistry timers)
        {
            _factory = factory;
            _timers = timers;
        }

        private IDisposable? _timer;

        public override async Task OnActivateAsync()
        {
            await TickUpdateAsync();

            _timer = _timers.RegisterTimer(this, TickUpdateAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _timer?.Dispose();

            return base.OnDeactivateAsync();
        }

        private async Task TickUpdateAsync(object? _ = default)
        {
            _ready = await _factory.GetBinanceMarketDataGrain().IsReadyAsync();
        }

        private bool _ready;

        public Task<bool> IsReadyAsync() => Task.FromResult(_ready);
    }
}