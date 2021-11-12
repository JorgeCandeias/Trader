using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal interface ISwapPoolGrain : IGrainWithGuidKey
    {
        ValueTask<bool> IsReadyAsync();

        ValueTask<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount);

        ValueTask<SwapPoolAssetBalance> GetBalanceAsync(string asset);

        ValueTask<IEnumerable<SwapPool>> GetSwapPoolsAsync();

        ValueTask<IEnumerable<SwapPoolConfiguration>> GetSwapPoolConfigurationsAsync();
    }
}