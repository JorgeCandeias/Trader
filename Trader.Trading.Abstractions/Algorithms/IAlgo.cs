using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <summary>
    /// Base interface for algos that do not follow the suggested lifecycle.
    /// Consider implementing the Algo class for less effort.
    /// </summary>
    public interface IAlgo
    {
        ValueTask StartAsync(CancellationToken cancellationToken = default);

        ValueTask StopAsync(CancellationToken cancellationToken = default);

        ValueTask GoAsync(CancellationToken cancellationToken = default);
    }
}