using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record Kline(
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
        public static IComparer<Kline> KeyComparer { get; } = new KeyComparerInternal();

        public static IEqualityComparer<Kline> KeyEqualityComparer { get; } = new KeyEqualityComparerInternal();

        private sealed class KeyEqualityComparerInternal : EqualityComparer<Kline>
        {
            public override bool Equals(Kline? x, Kline? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                else
                {
                    return y is not null
                        && EqualityComparer<string>.Default.Equals(x.Symbol, y.Symbol)
                        && EqualityComparer<KlineInterval>.Default.Equals(x.Interval, y.Interval)
                        && EqualityComparer<DateTime>.Default.Equals(x.OpenTime, y.OpenTime);
                }
            }

            public override int GetHashCode([DisallowNull] Kline obj)
            {
                if (obj is null) throw new ArgumentNullException(nameof(obj));

                return HashCode.Combine(obj.Symbol, obj.Interval, obj.OpenTime);
            }
        }

        private sealed class KeyComparerInternal : Comparer<Kline>
        {
            public override int Compare(Kline? x, Kline? y)
            {
                if (x is null)
                {
                    if (y is null)
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    if (y is null)
                    {
                        return 1;
                    }
                    else
                    {
                        var bySymbol = Comparer<string>.Default.Compare(x.Symbol, y.Symbol);
                        if (bySymbol != 0) return bySymbol;

                        var byInterval = Comparer<KlineInterval>.Default.Compare(x.Interval, y.Interval);
                        if (byInterval != 0) return byInterval;

                        return Comparer<DateTime>.Default.Compare(x.OpenTime, y.OpenTime);
                    }
                }
            }
        }

        public static Kline Empty { get; } = new Kline(string.Empty, KlineInterval.None, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, true, 0, 0);
    }
}