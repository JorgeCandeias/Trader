using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISavingsProvider
    {
        ValueTask<IEnumerable<SavingsPosition>> GetPositionsAsync(CancellationToken cancellationToken = default);

        ValueTask<SavingsPosition?> TryGetPositionAsync(string asset, CancellationToken cancellation = default);

        ValueTask<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default);

        ValueTask<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}