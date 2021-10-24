using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class NullAlgoResult : IAlgoResult
    {
        private NullAlgoResult()
        {
        }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public static NullAlgoResult Instance { get; } = new NullAlgoResult();
    }
}