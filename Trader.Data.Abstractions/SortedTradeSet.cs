using System;
using System.Collections.Generic;
using Trader.Models;

namespace Trader.Data
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

                var bySymbol = Comparer<string>.Default.Compare(x.Symbol, y.Symbol);
                if (bySymbol is not 0) return bySymbol;

                return Comparer<long>.Default.Compare(x.Id, y.Id);
            }

            public static TradeIdComparer Instance { get; } = new TradeIdComparer();
        }
    }
}