using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISavingsProvider
    {
        Task<IEnumerable<SavingsPosition>> GetPositionsAsync(CancellationToken cancellationToken = default);

        Task<SavingsPosition?> TryGetPositionAsync(string asset, CancellationToken cancellation = default);

        Task<SavingsQuota?> TryGetQuotaAsync(string asset, CancellationToken cancellationToken = default);

        Task<RedeemSavingsEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}