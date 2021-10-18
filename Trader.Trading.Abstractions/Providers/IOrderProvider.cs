using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProvider
    {
        Task SetOrdersAsync(string symbol, IReadOnlyCollection<OrderQueryResult> items, CancellationToken cancellationToken = default);

        Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);
    }
}