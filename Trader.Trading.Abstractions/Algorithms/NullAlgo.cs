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

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task GoAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}