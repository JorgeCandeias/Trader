using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProviderReplicaGrain : IGrainWithStringKey
    {
        [AlwaysInterleave]
        Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync();

        [AlwaysInterleave]
        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders);

        [AlwaysInterleave]
        Task SetOrderAsync(OrderQueryResult order);

        [AlwaysInterleave]
        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);
    }
}