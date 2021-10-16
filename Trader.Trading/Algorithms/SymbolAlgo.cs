using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public abstract class SymbolAlgo : IAlgo
    {
        public abstract ValueTask GoAsync(CancellationToken cancellationToken = default);
    }
}