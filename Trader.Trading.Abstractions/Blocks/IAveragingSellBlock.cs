using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    public interface IAveragingSellBlock
    {
        Task GoAsync(Symbol symbol, decimal profitMultiplier, CancellationToken cancellationToken = default);
    }
}