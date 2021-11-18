using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace System.Collections.Generic
{
    public static class KlineCollectionListExtensions
    {
        public static KlineCollection AsKlineCollection(this IList<Kline> klines)
        {
            return new KlineCollection(klines);
        }
    }
}