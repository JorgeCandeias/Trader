using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal class SavingsProvider : ISavingsProvider
    {
        private readonly IGrainFactory _factory;

        public SavingsProvider(IGrainFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public ValueTask<IReadOnlyList<SavingsProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain().GetProductsAsync();
        }

        public ValueTask<IEnumerable<SavingsPosition>> GetPositionsAsync(CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain().GetPositionsAsync();
        }

        public ValueTask<SavingsPosition?> TryGetPositionAsync(string asset, CancellationToken cancellation = default)
        {
            return _factory.GetSavingsGrain().TryGetPositionAsync(asset);
        }

        public ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain().TryGetQuotaAsync(asset);
        }

        public ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default)
        {
            return _factory.GetSavingsGrain().RedeemAsync(asset, amount);
        }
    }
}