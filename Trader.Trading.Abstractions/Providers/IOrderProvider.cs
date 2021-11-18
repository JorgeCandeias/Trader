using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers;

public interface IOrderProvider
{
    /// <summary>
    /// Publishes the specified order.
    /// </summary>
    ValueTask SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the specified order.
    /// </summary>
    ValueTask SetOrderAsync(OrderResult order, decimal stopPrice = 0m, decimal icebergQuantity = 0m, decimal originalQuoteOrderQuantity = 0m, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the specified order.
    /// </summary>
    ValueTask SetOrderAsync(CancelStandardOrderResult order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishers the specified orders.
    /// </summary>
    ValueTask SetOrdersAsync(string symbol, IEnumerable<OrderQueryResult> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders for the specified symbol sorted by <see cref="OrderQueryResult.OrderId"/>.
    /// </summary>
    ValueTask<OrderCollection> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets filtered orders for the specified symbol sorted by <see cref="OrderQueryResult.OrderId"/>.
    /// </summary>
    ValueTask<OrderCollection> GetOrdersByFilterAsync(string symbol, OrderSide? side, bool? transient, bool? significant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the order with the specified <paramref name="orderId"/> and <paramref name="symbol"/>.
    /// </summary>
    ValueTask<OrderQueryResult?> TryGetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default);
}