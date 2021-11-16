using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Orders;

public interface IOrderProviderReplicaGrain : IGrainWithStringKey
{
    Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync();

    Task<IReadOnlyList<OrderQueryResult>> GetOrdersByFilterAsync(OrderSide? side, bool? transient, bool? significant);

    Task<OrderQueryResult?> TryGetOrderAsync(long orderId);

    Task SetOrderAsync(OrderQueryResult item);

    Task SetOrdersAsync(IEnumerable<OrderQueryResult> items);
}