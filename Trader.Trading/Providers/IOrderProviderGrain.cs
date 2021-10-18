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
        Task SetOrderAsync(OrderQueryResult order);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders);

        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

        Task<(Guid Version, int MaxSerial, IReadOnlyList<OrderQueryResult> Orders)> GetOrdersAsync();

        [AlwaysInterleave]
        Task<(Guid Version, int MaxSerial, IReadOnlyList<OrderQueryResult> Orders)> PollOrdersAsync(Guid version, int fromSerial, GrainCancellationToken grainCancellationToken);
    }
}