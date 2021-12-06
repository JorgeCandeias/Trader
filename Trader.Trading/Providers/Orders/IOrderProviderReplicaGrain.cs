namespace Outcompute.Trader.Trading.Providers.Orders;

public interface IOrderProviderReplicaGrain : IGrainWithStringKey
{
    ValueTask<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync();

    ValueTask<ImmutableSortedSet<OrderQueryResult>> GetOrdersByFilterAsync(OrderSide? side, bool? transient, bool? significant);

    ValueTask<OrderQueryResult?> TryGetOrderAsync(long orderId);

    Task SetOrderAsync(OrderQueryResult item);

    Task SetOrdersAsync(IEnumerable<OrderQueryResult> items);
}