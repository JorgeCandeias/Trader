using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    public interface IOrderProviderReplicaGrain : IGrainWithStringKey
    {
        Task<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync();

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders);

        Task SetOrderAsync(OrderQueryResult order);

        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);
    }
}