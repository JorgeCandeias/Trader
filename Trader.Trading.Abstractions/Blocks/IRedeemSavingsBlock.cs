using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    public interface IRedeemSavingsBlock
    {
        Task<bool> GoAsync(string asset, decimal amount, CancellationToken cancellationToken = default);
    }
}