using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class NullAlgoResult : AlgoResult
    {
        private NullAlgoResult()
        {
        }

        public override Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public static NullAlgoResult Instance { get; } = new NullAlgoResult();
    }
}