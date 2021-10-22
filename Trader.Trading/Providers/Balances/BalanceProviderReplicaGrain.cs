using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Balances
{
    [Reentrant]
    [StatelessWorker(1)]
    internal class BalanceProviderReplicaGrain : Grain, IBalanceProviderReplicaGrain
    {
        private readonly ReactiveOptions _reactive;
        private readonly IGrainFactory _factory;
        private readonly IHostApplicationLifetime _lifetime;

        public BalanceProviderReplicaGrain(IOptions<ReactiveOptions> reactive, IGrainFactory factory, IHostApplicationLifetime lifetime)
        {
            _reactive = reactive.Value;
            _factory = factory;
            _lifetime = lifetime;
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

        public override async Task OnActivateAsync()
        {
            _asset = this.GetPrimaryKeyString();

            await LoadAsync();

            RegisterTimer(_ => PollAsync(), null, _reactive.ReactiveRecoveryDelay, _reactive.ReactiveRecoveryDelay);

            await base.OnActivateAsync();
        }

        public Task<Balance?> TryGetBalanceAsync()
        {
            return Task.FromResult(_balance);
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
}