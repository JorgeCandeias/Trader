using System;
using System.Collections.Generic;

namespace Trader.Data
{
    public class SortedOrderSet : SortedSet<OrderQueryResult>
    {
        public SortedOrderSet() : base(OrderIdComparer.Instance)
        {
        }

        private class OrderIdComparer : IComparer<OrderQueryResult>
        {
            private OrderIdComparer()
            {
            }

            public int Compare(OrderQueryResult? x, OrderQueryResult? y)
            {
                if (x is null) throw new ArgumentNullException(nameof(x));
                if (y is null) throw new ArgumentNullException(nameof(y));

                return x.OrderId.CompareTo(y.OrderId);
            }

            public static OrderIdComparer Instance { get; } = new OrderIdComparer();
        }
    }
}