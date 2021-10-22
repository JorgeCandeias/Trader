using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using OrleansDashboard;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Tickers
{
    [Reentrant]
    internal class TickerProviderGrain : Grain, ITickerProviderGrain
    {
        private readonly ReactiveOptions _reactive;
        private readonly ITradingRepository _repository;
        private readonly IHostApplicationLifetime _lifetime;

        public TickerProviderGrain(IOptions<ReactiveOptions> reactive, ITradingRepository repository, IHostApplicationLifetime lifetime)
        {
            _reactive = reactive.Value;
            _repository = repository;
            _lifetime = lifetime;
        }

        /// <summary>
        /// The symbol that this grain is responsible for.
        /// </summary>
        private string _symbol = null!;

        /// <summary>
        /// Holds the cached ticker.
        /// </summary>
        private MiniTicker? _ticker;

        /// <summary>
        /// An instance version that helps reset replicas upon reactivation of this grain.
        /// </summary>
        private Guid _version = Guid.NewGuid();

        /// <summary>
        /// Holds the promise for the next result.
        /// </summary>
        private TaskCompletionSource<ReactiveResult?> _completion = new();

        public override async Task OnActivateAsync()
        {
            _symbol = this.GetPrimaryKeyString();

            await LoadAsync();

            await base.OnActivateAsync();
        }

        public Task<MiniTicker?> TryGetTickerAsync()
        {
            return Task.FromResult(_ticker);
        }

        public Task<ReactiveResult> GetTickerAsync()
        {
            return Task.FromResult(new ReactiveResult(_version, _ticker));
        }

        [NoProfiling]
        public Task<ReactiveResult?> TryWaitForTickerAsync(Guid version)
        {
            // if the versions differ then return the entire data set
            if (version != _version)
            {
                return Task.FromResult<ReactiveResult?>(new ReactiveResult(_version, _ticker));
            }

            // otherwise let the request wait for the next result
            return _completion.Task.WithDefaultOnTimeout(null, _reactive.ReactivePollingTimeout, _lifetime.ApplicationStopping);
        }

        private async Task LoadAsync()
        {
            var ticker = await _repository.TryGetTickerAsync(_symbol, _lifetime.ApplicationStopping);

            Apply(ticker);
        }

        public Task SetTickerAsync(MiniTicker item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            return SetTickerCoreAsync(item);
        }

        private Task SetTickerCoreAsync(MiniTicker item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            Apply(item);

            return Task.CompletedTask;
        }

        private void Apply(MiniTicker? item)
        {
            _ticker = item;
            _version = Guid.NewGuid();

            Complete();
        }

        private void Complete()
        {
            _completion.SetResult(new ReactiveResult(_version, _ticker));
            _completion = new();
        }
    }
}