using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace System.Collections.Generic
{
    public static class ImmutableSortedTradeSetEnumerableExtensions
    {
        public static ImmutableSortedTradeSet ToImmutableSortedTradeSet(this IEnumerable<AccountTrade> items) => ImmutableSortedTradeSet.Create(items);
    }
}