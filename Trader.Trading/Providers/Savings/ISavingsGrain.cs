using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal interface ISavingsGrain : IGrainWithGuidKey
    {
        ValueTask<bool> IsReadyAsync();

        ValueTask<IReadOnlyList<SavingsProduct>> GetProductsAsync();

        ValueTask<IEnumerable<SavingsPosition>> GetPositionsAsync();

        ValueTask<SavingsPosition?> TryGetPositionAsync(string asset);

        ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset);

        ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount);
    }
}