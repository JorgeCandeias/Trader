using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal class SavingsProvider : ISavingsProvider
    {
        private readonly IGrainFactory _factory;

        public SavingsProvider(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public Task<IEnumerable<SavingsPosition>> GetPositionsAsync(CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain().GetPositionsAsync();
        }

        public Task<SavingsPosition?> TryGetPositionAsync(string asset, CancellationToken cancellation = default)
        {
            return _factory.GetSavingsGrain().TryGetPositionAsync(asset);
        }

        public Task<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain().TryGetQuotaAsync(asset);
        }

        public Task<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain().RedeemAsync(asset, amount);
        }
    }
}