using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface ISwapPoolProvider
    {
        Task<RedeemSwapPoolEvent> RedeemAsync(string asset, decimal amount, CancellationToken cancellationToken = default);

        Task<SwapPoolAssetBalance> GetBalanceAsync(string asset, CancellationToken cancellationToken = default);
    }
}