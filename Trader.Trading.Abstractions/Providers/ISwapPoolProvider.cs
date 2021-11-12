using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISwapPoolProvider
    {
        ValueTask<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default);

        ValueTask<SwapPoolAssetBalance> GetBalanceAsync(string asset, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<SwapPool>> GetSwapPoolsAsync(CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<SwapPoolConfiguration>> GetSwapPoolConfigurationsAsync(CancellationToken cancellationToken = default);
    }
}