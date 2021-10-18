using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProvider
    {
        ValueTask<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        ValueTask<long> GetMaxOrderIdAsync(string symbol, CancellationToken cancellationToken = default);

        ValueTask SetOrdersAsync(string symbol, IReadOnlyCollection<OrderQueryResult> items, CancellationToken cancellationToken = default);

        ValueTask SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

        ValueTask SetOrderAsync(OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default);

        ValueTask SetOrderAsync(CancelStandardOrderResult order, CancellationToken cancellationToken = default);

        ValueTask<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        ValueTask<IReadOnlyList<OrderQueryResult>> GetSignificantCompletedOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        ValueTask<IReadOnlyList<OrderQueryResult>> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default);

        ValueTask<IReadOnlyList<OrderQueryResult>> GetNonSignificantTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default);
    }
}