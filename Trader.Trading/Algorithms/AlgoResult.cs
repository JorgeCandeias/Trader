using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public abstract class AlgoResult : IAlgoResult
    {
        public static AlgoResult None { get; } = NullAlgoResult.Instance;

        public abstract Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default);
    }
}