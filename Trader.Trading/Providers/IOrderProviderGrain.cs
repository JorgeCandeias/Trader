using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProviderGrain : IGrainWithStringKey
    {
        ValueTask SetOrderAsync(OrderQueryResult order);

        ValueTask SetOrdersAsync(IReadOnlyCollection<OrderQueryResult> orders);

        ValueTask<OrderQueryResult?> TryGetOrderAsync(long orderId);

        ValueTask<(Guid Version, int MaxSerial, IReadOnlyList<OrderQueryResult> Orders)> GetOrdersAsync();

        [AlwaysInterleave]
        ValueTask<(Guid Version, int MaxSerial, IReadOnlyList<OrderQueryResult> Orders)> PollOrdersAsync(Guid version, int fromSerial);
    }
}