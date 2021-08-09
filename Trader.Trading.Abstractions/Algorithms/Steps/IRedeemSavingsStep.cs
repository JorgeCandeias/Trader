using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Algorithms.Steps
{
    public interface IRedeemSavingsStep
    {
        Task<bool> GoAsync(Symbol symbol, decimal amount, CancellationToken cancellationToken = default);
    }
}