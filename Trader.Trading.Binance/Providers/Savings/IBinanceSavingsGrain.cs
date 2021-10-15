using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.Savings
{
    internal interface IBinanceSavingsGrain : IGrainWithStringKey
    {
        ValueTask<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionsAsync();

        ValueTask<FlexibleProductPosition?> TryGetFirstFlexibleProductPositionAsync();

        ValueTask<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type);

        ValueTask RedeemFlexibleProductAsync(string productId, decimal amount, FlexibleProductRedemptionType type);
    }
}