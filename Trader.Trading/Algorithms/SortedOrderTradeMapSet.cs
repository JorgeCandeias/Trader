namespace Outcompute.Trader.Trading.Algorithms;

internal class SortedOrderTradeMapSet : SortedSet<OrderTradeMap>
{
    public SortedOrderTradeMapSet() : base(OrderTradeMapComparer.Instance)
    {
    }

    private sealed class OrderTradeMapComparer : IComparer<OrderTradeMap>
    {
        private OrderTradeMapComparer()
        {
        }

        public int Compare(OrderTradeMap? x, OrderTradeMap? y)
        {
            if (x is null) throw new ArgumentNullException(nameof(x));
            if (y is null) throw new ArgumentNullException(nameof(y));

            // keep the set sorted by max event time
            var byEventTime = x.MaxEventTime.CompareTo(y.MaxEventTime);
            if (byEventTime is not 0) return byEventTime;

            // resort to order id if needed
            return x.Order.OrderId.CompareTo(y.Order.OrderId);
        }

        public static OrderTradeMapComparer Instance { get; } = new OrderTradeMapComparer();
    }
}