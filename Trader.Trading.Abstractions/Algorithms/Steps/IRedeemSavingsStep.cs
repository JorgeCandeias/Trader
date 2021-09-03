using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Steps
{
    public interface IRedeemSavingsStep
    {
        Task<bool> GoAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}