using Orleans;
using Orleans.Concurrency;
using Orleans.Timers;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData;

/// <summary>
/// Actively pulls the readyness state of the <see cref="BinanceUserDataGrain"/> into each silo that needs it.
/// </summary>
[StatelessWorker(1)]
internal class BinanceUserDataReadynessGrain : Grain, IBinanceUserDataReadynessGrain
{
    private readonly IGrainFactory _factory;
    private readonly ITimerRegistry _timers;

    public BinanceUserDataReadynessGrain(IGrainFactory factory, ITimerRegistry timers)
    {
        _factory = factory;
        _timers = timers;
    }

    public override async Task OnActivateAsync()
    {
        await TickUpdateAsync();

        _timers.RegisterTimer(this, TickUpdateAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        await base.OnActivateAsync();
    }

    private async Task TickUpdateAsync(object? _ = default)
    {
        _ready = await _factory.GetBinanceUserDataGrain().IsReadyAsync();
    }

    private bool _ready;

    public ValueTask<bool> IsReadyAsync() => ValueTask.FromResult(_ready);
}