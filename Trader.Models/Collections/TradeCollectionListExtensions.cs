using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace System.Collections.Generic
{
    public static class TradeCollectionListExtensions
    {
        public static TradeCollection AsTradeCollection(this IList<AccountTrade> list)
        {
            return new TradeCollection(list);
        }
    }
}