using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.Savings
{
    internal interface IBinanceSavingsGrain : IGrainWithStringKey
    {
        Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync();

        Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type);

        Task RedeemFlexibleProductAsync(string productId, decimal amount, FlexibleProductRedemptionType type);
    }
}