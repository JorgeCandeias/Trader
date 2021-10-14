using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgo
    {
        ValueTask GoAsync(CancellationToken cancellationToken = default);
    }

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

        public ValueTask GoAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}