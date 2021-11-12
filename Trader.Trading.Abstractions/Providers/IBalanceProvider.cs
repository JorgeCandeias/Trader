using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IBalanceProvider
    {
        ValueTask<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default);

        ValueTask SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default);

        ValueTask SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default);
    }
}