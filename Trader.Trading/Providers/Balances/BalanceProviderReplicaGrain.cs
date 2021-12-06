using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Timers;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Balances;

[Reentrant]
[StatelessWorker(1)]
internal sealed class BalanceProviderReplicaGrain : Grain, IBalanceProviderReplicaGrain
{
    private readonly ReactiveOptions _reactive;
    private readonly IGrainActivationContext _context;
    private readonly IGrainFactory _factory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ITimerRegistry _timers;

    public BalanceProviderReplicaGrain(IOptions<ReactiveOptions> reactive, IGrainActivationContext context, IGrainFactory factory, IHostApplicationLifetime lifetime, ITimerRegistry timers)
    {
        _reactive = reactive.Value;
        _context = context;
        _factory = factory;
        _lifetime = lifetime;
        _timers = timers;
    }

    /// <summary>
    /// The asset that this grain is responsible for.
    /// </summary>
    private string _asset = null!;

    /// <summary>
    /// The version of the balance.
    /// </summary>
    private Guid _version;

    /// <summary>
    /// The balance cached by this grain.
    /// </summary>
    private Balance? _balance;

    /// <summary>
    /// Holds the polling timer.
    /// </summary>
    private IDisposable? _timer;

    public override async Task OnActivateAsync()
    {
        _asset = _context.GrainIdentity.PrimaryKeyString;

        await LoadAsync();

        _timer = _timers.RegisterTimer(this, _ => PollAsync(), null, _reactive.ReactiveRecoveryDelay, _reactive.ReactiveRecoveryDelay);

        await base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        _timer?.Dispose();

        return base.OnDeactivateAsync();
    }

    public ValueTask<Balance?> TryGetBalanceAsync()
    {
        return ValueTask.FromResult(_balance);
    }

    private async Task LoadAsync()
    {
        (_version, _balance) = await _factory.GetBalanceProviderGrain(_asset).GetBalanceAsync();
    }

    private async Task PollAsync()
    {
        while (!_lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                var result = await _factory.GetBalanceProviderGrain(_asset).TryWaitForBalanceAsync(_version);

                if (result.HasValue)
                {
                    _version = result.Value.Version;
                    _balance = result.Value.Value;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}