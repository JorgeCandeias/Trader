using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Balances
{
    internal class BalanceProvider : IBalanceProvider
    {
        private readonly IGrainFactory _factory;
        private readonly ITradingRepository _repository;

        public BalanceProvider(IGrainFactory factory, ITradingRepository repository)
        {
            _factory = factory;
            _repository = repository;
        }

        public Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return _factory.GetBalanceProviderReplicaGrain(asset).TryGetBalanceAsync();
        }

        public Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
        {
            if (balances is null) throw new ArgumentNullException(nameof(balances));

            return SetBalancesCoreAsync(balances);

            async Task SetBalancesCoreAsync(IEnumerable<Balance> balances)
            {
                await _repository.SetBalancesAsync(balances).ConfigureAwait(false);

                foreach (var balance in balances)
                {
                    await _factory.GetBalanceProviderGrain(balance.Asset).SetBalanceAsync(balance).ConfigureAwait(false);
                }
            }
        }
    }
}