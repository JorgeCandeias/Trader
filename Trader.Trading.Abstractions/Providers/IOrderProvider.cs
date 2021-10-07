using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProvider
    {
        Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
    }
}