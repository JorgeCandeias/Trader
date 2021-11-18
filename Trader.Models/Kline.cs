using Orleans.Concurrency;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models;

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
    public static Kline Empty { get; } = new Kline(string.Empty, KlineInterval.None, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, true, 0, 0);
}

public abstract class KlineComparer : IEqualityComparer<Kline>, IComparer<Kline>
{
    public static KlineComparer Key { get; } = new KlineKeyComparer();

    public abstract int Compare(Kline? x, Kline? y);

    public abstract bool Equals(Kline? x, Kline? y);

    public abstract int GetHashCode([DisallowNull] Kline obj);
}

internal class KlineKeyComparer : KlineComparer
{
    public override int Compare(Kline? x, Kline? y)
    {
        if (x is null) return y is null ? 0 : -1;
        if (y is null) return 1;

        var bySymbol = StringComparer.Ordinal.Compare(x.Symbol, y.Symbol);
        if (bySymbol != 0) return bySymbol;

        var byInterval = x.Interval.CompareTo(y.Interval);
        if (byInterval != 0) return byInterval;

        return x.OpenTime.CompareTo(y.OpenTime);
    }

    public override bool Equals(Kline? x, Kline? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;

        return
            StringComparer.Ordinal.Equals(x.Symbol, y.Symbol) &&
            x.Interval == y.Interval &&
            x.OpenTime == y.OpenTime;
    }

    public override int GetHashCode([DisallowNull] Kline obj)
    {
        return HashCode.Combine(obj.Symbol, obj.Interval, obj.OpenTime);
    }
}