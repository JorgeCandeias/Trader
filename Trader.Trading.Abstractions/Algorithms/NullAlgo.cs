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

        public ValueTask StartAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask StopAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask GoAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}