using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations
{
    public interface IClearOpenOrdersOperation
    {
        Task ClearOpenOrdersAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken = default);
    }
}