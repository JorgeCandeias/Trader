using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgo
    {
        Task GoAsync(CancellationToken cancellationToken = default);
    }
}