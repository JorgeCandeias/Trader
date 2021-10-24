using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations
{
    public interface IRedeemSavingsOperation
    {
        Task<(bool Success, decimal Redeemed)> TryRedeemSavingsAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}