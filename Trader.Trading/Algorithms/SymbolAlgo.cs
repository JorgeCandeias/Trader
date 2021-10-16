using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public abstract class SymbolAlgo : Algo, ISymbolAlgo
    {
        public override ValueTask GoAsync(CancellationToken cancellationToken = default)
        {
            return OnExecuteAsync(cancellationToken);
        }

        protected virtual ValueTask OnExecuteAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}