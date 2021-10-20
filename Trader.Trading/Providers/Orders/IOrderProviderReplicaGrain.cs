using Orleans;
using Outcompute.Trader.Models;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    public interface IOrderProviderReplicaGrain : IGrainWithStringKey
    {
        Task<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync();

        Task SetOrderAsync(OrderQueryResult order);

        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);
    }
}