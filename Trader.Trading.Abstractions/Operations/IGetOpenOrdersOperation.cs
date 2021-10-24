using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations
{
    public interface IGetOpenOrdersOperation
    {
        Task<IReadOnlyList<OrderQueryResult>> GetOpenOrdersAsync(Symbol symbol, OrderSide side, CancellationToken cancellationToken = default);
    }
}