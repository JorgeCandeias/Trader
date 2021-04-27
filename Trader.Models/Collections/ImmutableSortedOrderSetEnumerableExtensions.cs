using Trader.Models;
using Trader.Models.Collections;

namespace System.Collections.Generic
{
    public static class ImmutableSortedOrderSetEnumerableExtensions
    {
        public static ImmutableSortedOrderSet ToImmutableSortedOrderSet(this IEnumerable<OrderQueryResult> items) => ImmutableSortedOrderSet.Create(items);
    }
}