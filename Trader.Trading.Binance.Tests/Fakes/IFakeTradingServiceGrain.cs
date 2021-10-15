using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    internal interface IFakeTradingServiceGrain : IGrainWithGuidKey
    {
        Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionsAsync(string asset);

        Task SetFlexibleProductPositionsAsync(IEnumerable<FlexibleProductPosition> items);

        Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(
            string productId,
            FlexibleProductRedemptionType type);

        public Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, FlexibleProductRedemptionType type, LeftDailyRedemptionQuotaOnFlexibleProduct item);
    }
}