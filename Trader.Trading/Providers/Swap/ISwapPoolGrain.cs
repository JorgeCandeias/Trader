using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal interface ISwapPoolGrain : IGrainWithGuidKey
    {
        Task<bool> IsReadyAsync();

        Task<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount);

        Task<SwapPoolAssetBalance> GetBalanceAsync(string asset);

        Task<IEnumerable<SwapPool>> GetSwapPoolsAsync();
    }
}