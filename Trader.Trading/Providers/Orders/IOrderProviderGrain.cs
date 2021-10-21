using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    public interface IOrderProviderGrain : IGrainWithStringKey
    {
        Task SetOrderAsync(OrderQueryResult order);

        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

        Task<(Guid Version, int MaxSerial, ImmutableSortedSet<OrderQueryResult> Orders)> GetOrdersAsync();

        Task<(Guid Version, int MaxSerial, ImmutableSortedSet<OrderQueryResult> Orders)> GetOrdersAsync(Guid version, int fromSerial);
    }
}