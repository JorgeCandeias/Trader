using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Algorithms.Context
{
    /// <summary>
    /// Organizes historical orders for a symbol.
    /// </summary>
    public class SymbolOrders
    {
        /// <summary>
        /// All current open orders in the exchange.
        /// </summary>
        public ImmutableSortedSet<OrderQueryResult> Open { get; set; } = ImmutableSortedSet<OrderQueryResult>.Empty.WithComparer(OrderQueryResult.KeyComparer);

        /// <summary>
        /// All completed orders with positive executed quantity.
        /// </summary>
        public ImmutableSortedSet<OrderQueryResult> Filled { get; set; } = ImmutableSortedSet<OrderQueryResult>.Empty.WithComparer(OrderQueryResult.KeyComparer);

        /// <summary>
        /// All completed orders including canceled orders.
        /// </summary>
        public ImmutableSortedSet<OrderQueryResult> Completed { get; set; } = ImmutableSortedSet<OrderQueryResult>.Empty.WithComparer(OrderQueryResult.KeyComparer);
    }
}