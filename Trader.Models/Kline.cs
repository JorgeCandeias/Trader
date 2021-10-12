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
    }
}