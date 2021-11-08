using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Savings
{
    internal interface ISavingsGrain : IGrainWithGuidKey
    {
        Task<bool> IsReadyAsync();

        Task<SavingsPosition?> TryGetPositionAsync(string asset);

        Task<SavingsQuota?> TryGetQuotaAsync(string asset);

        Task<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount);
    }
}