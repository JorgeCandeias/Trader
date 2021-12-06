﻿using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers.Orders;

public interface IOrderProviderReplicaGrain : IGrainWithStringKey
{
    ValueTask<ImmutableSortedOrderSet> GetOrdersAsync();

    ValueTask<ImmutableSortedOrderSet> GetOrdersByFilterAsync(OrderSide? side, bool? transient, bool? significant);

    ValueTask<OrderQueryResult?> TryGetOrderAsync(long orderId);

    Task SetOrderAsync(OrderQueryResult item);

    Task SetOrdersAsync(IEnumerable<OrderQueryResult> items);
}