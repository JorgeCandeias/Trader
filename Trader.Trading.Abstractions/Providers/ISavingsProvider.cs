using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISavingsProvider
    {
        ValueTask<IReadOnlyCollection<FlexibleProductPosition>> GetFlexibleProductPositionAsync(string asset, CancellationToken cancellationToken = default);

        ValueTask<FlexibleProductPosition?> TryGetFirstFlexibleProductPositionAsync(string asset, CancellationToken cancellation = default);

        ValueTask<LeftDailyRedemptionQuotaOnFlexibleProduct?> TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string asset, string productId, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default);

        ValueTask RedeemFlexibleProductAsync(string asset, string productId, decimal amount, FlexibleProductRedemptionType type, CancellationToken cancellationToken = default);
    }
}