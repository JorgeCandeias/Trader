using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    public interface IOrderProviderReplicaGrain : IGrainWithStringKey
    {
        Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync();

        Task<IReadOnlyList<OrderQueryResult>> GetOrdersByFilterAsync(OrderSide? side, bool? isTransient);

        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

        Task SetOrderAsync(OrderQueryResult item);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> items);
    }
}