using Trader.Models;
using Trader.Models.Collections;

namespace System.Collections.Generic
{
    public static class ImmutableSortedTradeSetEnumerableExtensions
    {
        public static ImmutableSortedTradeSet ToImmutableSortedTradeSet(this IEnumerable<AccountTrade> items) => ImmutableSortedTradeSet.Create(items);
    }
}