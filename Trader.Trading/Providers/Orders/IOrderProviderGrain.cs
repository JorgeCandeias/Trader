using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Orders;

internal interface IOrderProviderGrain : IGrainWithStringKey
{
    Task<ReactiveResult> GetOrdersAsync();

    Task<ReactiveResult?> TryWaitForOrdersAsync(Guid version, int fromSerial);

    Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

    Task SetOrderAsync(OrderQueryResult item);

    Task SetOrdersAsync(IEnumerable<OrderQueryResult> items);
}