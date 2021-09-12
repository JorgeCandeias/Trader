using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// An empty algo that does nothing.
    /// For use with non-nullable variables and testing.
    /// </summary>
    public class NullAlgo : IAlgo
    {
        private NullAlgo()
        {
        }

        public Task GoAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static NullAlgo Instance { get; } = new NullAlgo();
    }
}