using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IOrderSynchronizer
    {
        Task SynchronizeOrdersAsync(string symbol, CancellationToken cancellationToken = default);
    }
}