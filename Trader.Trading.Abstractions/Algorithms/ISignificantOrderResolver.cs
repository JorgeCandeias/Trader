using System.Threading;
using System.Threading.Tasks;
using Trader.Data;

namespace Trader.Trading.Algorithms
{
    public interface ISignificantOrderResolver
    {
        Task<SortedOrderSet> ResolveAsync(string symbol, CancellationToken cancellationToken = default);
    }
}