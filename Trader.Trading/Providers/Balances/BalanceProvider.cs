using AutoMapper;
using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Balances
{
    // todo: refactor this class so all balances are queried from local replica grains
    internal class BalanceProvider : IBalanceProvider
    {
        private readonly IGrainFactory _factory;
        private readonly ITradingRepository _repository;
        private readonly IMapper _mapper;

        public BalanceProvider(IGrainFactory factory, ITradingRepository repository, IMapper mapper)
        {
            _factory = factory;
            _repository = repository;
            _mapper = mapper;
        }

        public Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return _factory.GetBalanceProviderReplicaGrain(asset).TryGetBalanceAsync();
        }

        public Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
        {
            if (balances is null) throw new ArgumentNullException(nameof(balances));

            return SetBalancesCoreAsync(balances, cancellationToken);
        }

        private async Task SetBalancesCoreAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
        {
            await _repository.SetBalancesAsync(balances, cancellationToken).ConfigureAwait(false);

            await balances
                .Select(balance => _factory.GetBalanceProviderGrain(balance.Asset).SetBalanceAsync(balance))
                .WhenAll()
                .ConfigureAwait(false);
        }

        public Task SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            if (accountInfo is null) throw new ArgumentNullException(nameof(accountInfo));

            var balances = _mapper.Map<IEnumerable<Balance>>(accountInfo);

            return SetBalancesAsync(balances, cancellationToken);
        }

        public Task<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
        {
            return _repository.GetBalancesAsync(cancellationToken);
        }
    }
}