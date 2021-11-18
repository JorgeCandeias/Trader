using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace System.Collections.Generic
{
    public static class OrderCollectionEnumerableExtensions
    {
        public static OrderCollection ToOrderCollection(this IEnumerable<OrderQueryResult> orders)
        {
            var list = orders.ToList();

            return new OrderCollection(list);
        }
    }
}