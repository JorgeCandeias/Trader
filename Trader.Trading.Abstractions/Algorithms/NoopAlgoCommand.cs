using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class NoopAlgoCommand : IAlgoCommand
    {
        private NoopAlgoCommand()
        {
        }

        public Task ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public static NoopAlgoCommand Instance { get; } = new NoopAlgoCommand();
    }
}