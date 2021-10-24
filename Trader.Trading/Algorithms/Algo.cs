using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// Base class for algos that do not follow the suggested lifecycle.
    /// For symbol based algos, consider implementing <see cref="SymbolAlgo"/> instead.
    /// </summary>
    public abstract class Algo : IAlgo
    {
        public abstract ValueTask GoAsync(CancellationToken cancellationToken = default);

        public virtual ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public IAlgoContext Context { get; set; } = NullAlgoContext.Instance;
    }
}