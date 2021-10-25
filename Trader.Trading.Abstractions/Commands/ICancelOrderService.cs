using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface ICancelOrderService
    {
        Task<CancelStandardOrderResult> CancelOrderAsync(Symbol symbol, long orderId, CancellationToken cancellationToken = default);
    }
}