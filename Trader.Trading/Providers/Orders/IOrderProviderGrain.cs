using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Orders;

internal interface IOrderProviderGrain : IGrainWithStringKey
{
    Task<long> GetLastSyncedOrderId();

    Task SetLastSyncedOrderId(long orderId);

    Task<ReactiveResult> GetOrdersAsync();

    Task<ReactiveResult?> TryWaitForOrdersAsync(Guid version, int fromSerial);

    Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

    Task SetOrderAsync(OrderQueryResult item);

    Task SetOrdersAsync(IEnumerable<OrderQueryResult> items);
}