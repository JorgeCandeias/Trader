using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Algorithms.Context
{
    /// <summary>
    /// Organizes historical orders for a symbol.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "N/A")]
    public class SymbolOrders
    {
        /// <summary>
        /// All current open orders in the exchange.
        /// </summary>
        public OrderCollection Open { get; set; } = OrderCollection.Empty;

        /// <summary>
        /// All completed orders with positive executed quantity.
        /// </summary>
        public OrderCollection Filled { get; set; } = OrderCollection.Empty;

        /// <summary>
        /// All completed orders including canceled orders.
        /// </summary>
        public OrderCollection Completed { get; set; } = OrderCollection.Empty;
    }
}