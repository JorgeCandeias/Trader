using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using OrleansDashboard;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Balances;

[Reentrant]
internal class BalanceProviderGrain : Grain, IBalanceProviderGrain
{
    private readonly ReactiveOptions _reactive;
    private readonly ITradingRepository _repository;
    private readonly IHostApplicationLifetime _lifetime;

    public BalanceProviderGrain(IOptions<ReactiveOptions> reactive, ITradingRepository repository, IHostApplicationLifetime lifetime)
    {
        _reactive = reactive.Value;
        _repository = repository;
        _lifetime = lifetime;
    }

    /// <summary>
    /// The asset that this grain is responsible for.
    /// </summary>
    private string _asset = null!;

    /// <summary>
    /// Holds the cached balance.
    /// </summary>
    private Balance? _balance;

    /// <summary>
    /// Holds the version of cached data.
    /// </summary>
    private Guid _version = Guid.NewGuid();

    /// <summary>
    /// Holds the promise for the next result.
    /// </summary>
    private TaskCompletionSource<ReactiveResult?> _completion = new();

    public override async Task OnActivateAsync()
    {
        _asset = this.GetPrimaryKeyString();

        await LoadAsync();

        await base.OnActivateAsync();
    }

    public Task<Balance?> TryGetBalanceAsync()
    {
        return Task.FromResult(_balance);
    }

    public Task<ReactiveResult> GetBalanceAsync()
    {
        return Task.FromResult(new ReactiveResult(_version, _balance));
    }

    [NoProfiling]
    public Task<ReactiveResult?> TryWaitForBalanceAsync(Guid version)
    {
        // if the versions differ then return the current balance
        if (version != _version)
        {
            return Task.FromResult<ReactiveResult?>(new ReactiveResult(_version, _balance));
        }

        // otherwise let the request wait for the next balance
        return _completion.Task.WithDefaultOnTimeout(null, _reactive.ReactivePollingTimeout, _lifetime.ApplicationStopping);
    }

    private async Task LoadAsync()
    {
        var balance = await _repository.TryGetBalanceAsync(_asset, _lifetime.ApplicationStopping);

        Apply(balance);
    }

    public Task SetBalanceAsync(Balance balance)
    {
        if (balance.Asset != _asset) throw new ArgumentOutOfRangeException(nameof(balance));

        Apply(balance);

        return Task.CompletedTask;
    }

    private void Apply(Balance? balance)
    {
        _balance = balance;
        _version = Guid.NewGuid();

        Complete();
    }

    private void Complete()
    {
        _completion.SetResult(new ReactiveResult(_version, _balance));
        _completion = new();
    }
}