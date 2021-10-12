using Orleans;
using Orleans.Concurrency;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    /// <summary>
    /// Actively pulls the readyness state of the <see cref="BinanceMarketDataGrain"/> into each silo that needs it.
    /// </summary>
    [StatelessWorker(1)]
    internal class BinanceMarketDataReadynessGrain : Grain, IBinanceMarketDataReadynessGrain
    {
        private readonly IGrainFactory _factory;

        public BinanceMarketDataReadynessGrain(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public override async Task OnActivateAsync()
        {
            await TickUpdateAsync();

            RegisterTimer(TickUpdateAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            await base.OnActivateAsync();
        }

        private async Task TickUpdateAsync(object? _ = default)
        {
            _ready = await _factory.GetBinanceMarketDataGrain().IsReadyAsync();
        }

        private bool _ready;

        public ValueTask<bool> IsReadyAsync() => new(_ready);
    }
}