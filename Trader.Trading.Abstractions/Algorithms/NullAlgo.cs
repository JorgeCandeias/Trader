using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// An empty algo that does nothing.
    /// For use with non nullable fields and unit testing.
    /// </summary>
    public class NullAlgo : IAlgo
    {
        private NullAlgo()
        {
        }

        public static NullAlgo Instance { get; } = new NullAlgo();

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IAlgoCommand>(NoopAlgoCommand.Instance);
        }
    }
}