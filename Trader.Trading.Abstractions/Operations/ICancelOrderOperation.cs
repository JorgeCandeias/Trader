using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations
{
    public interface ICancelOrderOperation
    {
        Task<CancelStandardOrderResult> CancelOrderAsync(Symbol symbol, long orderId, CancellationToken cancellationToken = default);
    }
}