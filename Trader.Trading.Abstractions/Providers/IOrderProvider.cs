using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers;

public interface IOrderProvider
{
    /// <summary>
    /// Gets the last saved synced order id for the specified symbol.
    /// </summary>
    Task<long> GetLastSyncedOrderId(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the specified order id as the last synced order for the specified symbol.
    /// </summary>
    Task SetLastSyncedOrderId(string symbol, long orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the specified order.
    /// </summary>
    Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the specified order.
    /// </summary>
    Task SetOrderAsync(OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the specified order.
    /// </summary>
    Task SetOrderAsync(CancelStandardOrderResult order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishers the specified orders.
    /// </summary>
    Task SetOrdersAsync(string symbol, IEnumerable<OrderQueryResult> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for the specified symbol sorted by <see cref="OrderQueryResult.OrderId"/>.
    /// </summary>
    ValueTask<ImmutableSortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets filtered orders for the specified symbol sorted by <see cref="OrderQueryResult.OrderId"/>.
    /// </summary>
    ValueTask<ImmutableSortedOrderSet> GetOrdersByFilterAsync(string symbol, OrderSide? side, bool? transient, bool? significant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order with the specified <paramref name="orderId"/> and <paramref name="symbol"/>.
    /// </summary>
    ValueTask<OrderQueryResult?> TryGetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default);
}