using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Steps
{
    public interface IAveragingSellStep
    {
        Task GoAsync(Symbol symbol, decimal profitMultiplier, CancellationToken cancellationToken = default);
    }
}