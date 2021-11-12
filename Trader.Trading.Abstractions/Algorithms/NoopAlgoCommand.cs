using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public class NoopAlgoCommand : IAlgoCommand
    {
        private NoopAlgoCommand()
        {
        }

        public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public static NoopAlgoCommand Instance { get; } = new NoopAlgoCommand();
    }
}