using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Trading.Algorithms;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.ProfitAggregator
{
    [StatelessWorker(1)]
    internal class ProfitAggregatorLocalGrain : Grain, IProfitAggregatorLocalGrain
    {
        private readonly IGrainFactory _factory;

        public ProfitAggregatorLocalGrain(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private readonly Dictionary<string, Profit> _cache = new();

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => UploadAsync(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

            return base.OnActivateAsync();
        }

        public Task PublishAsync(Profit profit)
        {
            if (profit is null) throw new ArgumentNullException(nameof(profit));

            _cache[profit.Symbol] = profit;

            return Task.CompletedTask;
        }

        private async Task UploadAsync()
        {
            // break early if there is nothing to upload
            if (_cache.Count is 0) return;

            // copy the items over from cache
            var builder = ImmutableList.CreateBuilder<Profit>();
            foreach (var item in _cache)
            {
                builder.Add(item.Value);
            }
            var list = builder.ToImmutable();

            // upload the items to the central grain
            // this call will interleave due to the orleans timer behaviour so the cache can change while this happens
            await _factory
                .GetProfitAggregatorGrain()
                .PublishAsync(list)
                .ConfigureAwait(true);

            // remove from cache only the items successfully uploaded
            // the cache may have changed and have new items already due to interleaving the previous call
            foreach (var item in list)
            {
                if (_cache.TryGetValue(item.Symbol, out var current) && current == item)
                {
                    _cache.Remove(item.Symbol);
                }
            }
        }
    }
}