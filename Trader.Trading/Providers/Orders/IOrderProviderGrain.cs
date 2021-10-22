using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    internal interface IOrderProviderGrain : IGrainWithStringKey
    {
        Task<ReactiveResult> GetOrdersAsync();

        Task<ReactiveResult?> TryWaitForOrdersAsync(Guid version, int fromSerial);

        Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

        Task SetOrderAsync(OrderQueryResult item);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> items);
    }
}