using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Readyness
{
    public interface IReadynessProvider
    {
        /// <summary>
        /// Return true if the system is in ready state, meaning all background services are active and healthy, otherwise false.
        /// </summary>
        ValueTask<bool> IsReadyAsync(CancellationToken cancellationToken = default);
    }
}