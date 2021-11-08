using Orleans;
using Orleans.Concurrency;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData
{
    /// <summary>
    /// Actively pulls the readyness state of the <see cref="BinanceUserDataGrain"/> into each silo that needs it.
    /// </summary>
    [StatelessWorker(1)]
    internal class BinanceUserDataReadynessGrain : Grain, IBinanceUserDataReadynessGrain
    {
        private readonly IGrainFactory _factory;

        public BinanceUserDataReadynessGrain(IGrainFactory factory)
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
            _ready = await _factory.GetBinanceUserDataGrain().IsReadyAsync();
        }

        private bool _ready;

        public Task<bool> IsReadyAsync() => Task.FromResult(_ready);
    }
}