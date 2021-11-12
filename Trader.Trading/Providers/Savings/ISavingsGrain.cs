using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal interface ISavingsGrain : IGrainWithGuidKey
    {
        ValueTask<bool> IsReadyAsync();

        ValueTask<IEnumerable<SavingsPosition>> GetPositionsAsync();

        ValueTask<SavingsPosition?> TryGetPositionAsync(string asset);

        ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset);

        ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount);
    }
}