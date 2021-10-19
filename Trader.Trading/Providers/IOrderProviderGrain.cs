using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProviderGrain : IGrainWithStringKey
    {
        Task SetOrderAsync(OrderQueryResult order);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders);

        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

        Task<(Guid Version, int MaxSerial, ImmutableSortedSet<OrderQueryResult> Orders)> GetOrdersAsync();

        Task<(Guid Version, int MaxSerial, ImmutableSortedSet<OrderQueryResult> Orders)> GetOrdersAsync(Guid version, int fromSerial);
    }
}