using Orleans;
using Outcompute.Trader.Models;
using System;
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

        public Task<SavingsPosition?> TryGetPositionAsync(string asset, CancellationToken cancellation = default)
        {
            return _factory.GetSavingsGrain(asset).TryGetPositionAsync();
        }

        public Task<SavingsQuota?> TryGetQuotaAsync(string asset, string productId, SavingsRedemptionType type, CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain(asset).TryGetQuotaAsync(productId, type);
        }

        public Task RedeemAsync(string asset, string productId, decimal amount, SavingsRedemptionType type, CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain(asset).RedeemAsync(productId, amount, type);
        }
    }
}