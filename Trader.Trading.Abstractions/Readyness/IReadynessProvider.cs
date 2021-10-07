using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Readyness
{
    public interface IReadynessProvider
    {
        Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
    }
}