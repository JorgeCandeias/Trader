using Orleans;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    internal interface ISwapPoolGrain : IGrainWithGuidKey
    {
        Task PingAsync();

        Task<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount);

        Task<SwapPoolAssetBalance> GetBalanceAsync(string asset);
    }
}