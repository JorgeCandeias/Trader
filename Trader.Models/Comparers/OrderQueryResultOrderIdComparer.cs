using System;
using System.Collections.Generic;

namespace Trader.Models.Comparers
{
    public class OrderQueryResultOrderIdComparer : IComparer<OrderQueryResult>
    {
        private OrderQueryResultOrderIdComparer()
        {
        }

        public int Compare(OrderQueryResult? x, OrderQueryResult? y)
        {
            if (x is null) throw new ArgumentNullException(nameof(x));
            if (y is null) throw new ArgumentNullException(nameof(y));

            var bySymbol = Comparer<string>.Default.Compare(x.Symbol, y.Symbol);
            if (bySymbol is not 0) return bySymbol;

            return Comparer<long>.Default.Compare(x.OrderId, y.OrderId);
        }

        public static OrderQueryResultOrderIdComparer Instance { get; } = new OrderQueryResultOrderIdComparer();
    }
}