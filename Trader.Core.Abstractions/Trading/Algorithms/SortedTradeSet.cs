using System;
using System.Collections.Generic;

namespace Trader.Core.Trading.Algorithms
{
    public class SortedTradeSet : SortedSet<AccountTrade>
    {
        public SortedTradeSet() : base(TradeIdComparer.Instance)
        {
        }

        private class TradeIdComparer : IComparer<AccountTrade>
        {
            private TradeIdComparer()
            {
            }

            public int Compare(AccountTrade? x, AccountTrade? y)
            {
                if (x is null) throw new ArgumentNullException(nameof(x));
                if (y is null) throw new ArgumentNullException(nameof(y));

                return x.Id.CompareTo(y.Id);
            }

            public static TradeIdComparer Instance { get; } = new TradeIdComparer();
        }
    }
}