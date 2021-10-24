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
        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);

        Task GoAsync(CancellationToken cancellationToken = default);
    }
}