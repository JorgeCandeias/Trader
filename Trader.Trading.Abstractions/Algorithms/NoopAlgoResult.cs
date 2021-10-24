using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class NoopAlgoResult : IAlgoResult
    {
        private NoopAlgoResult()
        {
        }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public static NoopAlgoResult Instance { get; } = new NoopAlgoResult();
    }
}