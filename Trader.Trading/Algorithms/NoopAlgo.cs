using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// An empty algo that does nothing.
    /// For use with non nullable fields and unit testing.
    /// </summary>
    public class NoopAlgo : IAlgo
    {
        private NoopAlgo()
        {
        }

        public static NoopAlgo Instance { get; } = new NoopAlgo();

        public IAlgoContext Context => AlgoContext.Empty;

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