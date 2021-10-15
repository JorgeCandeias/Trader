using Orleans.Concurrency;
using System;
using System.Collections.Generic;

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
        public static IComparer<Kline> OpenTimeComparer { get; } = new OpenTimeComparerInternal();

        private sealed class OpenTimeComparerInternal : Comparer<Kline>
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
                        return DateTime.Compare(x.OpenTime, y.OpenTime);
                    }
                }
            }
        }

        public static IComparer<Kline> KeyComparer { get; } = new KeyComparerInternal();

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