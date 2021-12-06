using Outcompute.Trader.Models.Collections;

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
        public ImmutableSortedOrderSet Open { get; set; } = ImmutableSortedOrderSet.Empty;

        /// <summary>
        /// All completed orders with positive executed quantity.
        /// </summary>
        public ImmutableSortedOrderSet Filled { get; set; } = ImmutableSortedOrderSet.Empty;

        /// <summary>
        /// All completed orders including canceled orders.
        /// </summary>
        public ImmutableSortedOrderSet Completed { get; set; } = ImmutableSortedOrderSet.Empty;
    }
}