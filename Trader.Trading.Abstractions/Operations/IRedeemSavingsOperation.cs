using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations
{
    public interface IRedeemSavingsOperation
    {
        Task<RedeemSavingsOperationResult> TryRedeemSavingsAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}