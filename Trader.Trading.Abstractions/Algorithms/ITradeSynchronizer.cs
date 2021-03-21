using System.Threading;
using System.Threading.Tasks;

namespace Trader.Trading.Algorithms
{
    public interface ITradeSynchronizer
    {
        Task SynchronizeTradesAsync(string symbol, CancellationToken cancellationToken = default);
    }
}