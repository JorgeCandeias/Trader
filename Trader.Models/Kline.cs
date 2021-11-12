using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public readonly record struct Kline(
        string Symbol,
        KlineInterval Interval,
        DateTime OpenTime,
        DateTime CloseTime,
        DateTime EventTime,
        long FirstTradeId,
        long LastTradeId,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal ClosePrice,
        decimal Volume,
        decimal QuoteAssetVolume,
        int TradeCount,
        bool IsClosed,
        decimal TakerBuyBaseAssetVolume,
        decimal TakerBuyQuoteAssetVolume)
    {
        public static Kline Empty { get; } = new Kline(string.Empty, KlineInterval.None, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, true, 0, 0);
    }

    public abstract class KlineComparer : IEqualityComparer<Kline>, IComparer<Kline>
    {
        public static KlineComparer Key { get; } = new KlineKeyComparer();

        public abstract int Compare(Kline x, Kline y);

        public abstract bool Equals(Kline x, Kline y);

        public abstract int GetHashCode([DisallowNull] Kline obj);
    }

    internal class KlineKeyComparer : KlineComparer
    {
        public override int Compare(Kline x, Kline y)
        {
            var bySymbol = Comparer<string>.Default.Compare(x.Symbol, y.Symbol);
            if (bySymbol != 0) return bySymbol;

            var byInterval = Comparer<KlineInterval>.Default.Compare(x.Interval, y.Interval);
            if (byInterval != 0) return byInterval;

            return Comparer<DateTime>.Default.Compare(x.OpenTime, y.OpenTime);
        }

        public override bool Equals(Kline x, Kline y)
        {
            return
                EqualityComparer<string>.Default.Equals(x.Symbol, y.Symbol) &&
                EqualityComparer<KlineInterval>.Default.Equals(x.Interval, y.Interval) &&
                EqualityComparer<DateTime>.Default.Equals(x.OpenTime, y.OpenTime);
        }

        public override int GetHashCode([DisallowNull] Kline obj)
        {
            return HashCode.Combine(obj.Symbol, obj.Interval, obj.OpenTime);
        }
    }
}