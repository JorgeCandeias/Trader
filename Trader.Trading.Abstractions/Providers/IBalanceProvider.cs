using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IBalanceProvider
    {
        Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default);

        Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default);

        Task SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default);

        Task<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default);
    }
}