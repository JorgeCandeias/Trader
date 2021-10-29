using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    public interface IOrderProvider
    {
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
        Task<IReadOnlyList<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets filtered orders for the specified symbol sorted by <see cref="OrderQueryResult.OrderId"/>.
        /// </summary>
        Task<IReadOnlyList<OrderQueryResult>> GetOrdersByFilterAsync(string symbol, OrderSide? side, bool? isTransient, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the order with the specified <paramref name="orderId"/> and <paramref name="symbol"/>.
        /// </summary>
        Task<OrderQueryResult?> TryGetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default);
    }
}