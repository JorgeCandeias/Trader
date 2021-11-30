using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Tickers;

[Reentrant]
[StatelessWorker(1)]
internal class TickerProviderReplicaGrain : Grain, ITickerProviderReplicaGrain
{
    private readonly ReactiveOptions _reactive;
    private readonly IGrainFactory _factory;
    private readonly ITradingRepository _repository;
    private readonly IHostApplicationLifetime _lifetime;

    public TickerProviderReplicaGrain(IOptions<ReactiveOptions> reactive, IGrainFactory factory, ITradingRepository repository, IHostApplicationLifetime lifetime)
    {
        _reactive = reactive.Value;
        _factory = factory;
        _repository = repository;
        _lifetime = lifetime;
    }

    /// <summary>
    /// The symbol that this grain holds orders for.
    /// </summary>
    private string _symbol = null!;

    /// <summary>
    /// The serial version of this grain.
    /// Helps detect serial resets from the source grain.
    /// </summary>
    private Guid _version;

    /// <summary>
    /// Holds the cached ticker.
    /// </summary>
    private MiniTicker? _ticker;

    public override async Task OnActivateAsync()
    {
        _symbol = this.GetPrimaryKeyString();

        await LoadAsync();

        RegisterTimer(_ => PollAsync(), null, _reactive.ReactiveRecoveryDelay, _reactive.ReactiveRecoveryDelay);

        await base.OnActivateAsync();
    }

    public Task<MiniTicker?> TryGetTickerAsync()
    {
        return Task.FromResult(_ticker);
    }

    public async Task SetTickerAsync(MiniTicker item)
    {
        Guard.IsNotNull(item, nameof(item));

        await _repository.SetTickerAsync(item, _lifetime.ApplicationStopping);

        await _factory.GetTickerProviderGrain(_symbol).SetTickerAsync(item);

        (_version, _ticker) = await _factory.GetTickerProviderGrain(_symbol).GetTickerAsync();
    }

    private async Task LoadAsync()
    {
        (_version, _ticker) = await _factory.GetTickerProviderGrain(_symbol).GetTickerAsync();
    }

    private async Task PollAsync()
    {
        while (!_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                var result = await _factory.GetTickerProviderGrain(_symbol).TryWaitForTickerAsync(_version);

                if (result.HasValue)
                {
                    (_version, _ticker) = result.Value;
                }
            }
            catch (OperationCanceledException)
            {
                // throw on target shutdown
                return;
            }
        }
    }
}