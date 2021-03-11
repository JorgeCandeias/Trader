using System;
using System.Collections.Generic;

namespace Trader.Core.Trading.Algorithms.Step
{
    internal class OrderQueryResultOrderIdComparer : IComparer<OrderQueryResult>
    {
        private readonly bool _ascending;

        public OrderQueryResultOrderIdComparer(bool ascending = true)
        {
            _ascending = ascending;
        }

        public int Compare(OrderQueryResult? x, OrderQueryResult? y)
        {
            _ = x ?? throw new ArgumentNullException(nameof(x));
            _ = y ?? throw new ArgumentNullException(nameof(y));

            return (_ascending ? 1 : -1) * (x.OrderId < y.OrderId ? -1 : x.OrderId > y.OrderId ? 1 : 0);
        }
    }
}