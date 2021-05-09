using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Algorithms.Steps
{
    public interface IAveragingSellStep
    {
        Task GoAsync(Symbol symbol, decimal profitMultiplier, CancellationToken cancellationToken = default);
    }
}