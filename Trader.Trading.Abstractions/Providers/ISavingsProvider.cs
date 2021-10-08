using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISavingsProvider
    {
        Task<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync(string asset, CancellationToken cancellationToken = default);

        Task<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string asset, string productId, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default);

        Task RedeemFlexibleProductAsync(string asset, string productId, decimal amount, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default);
    }
}