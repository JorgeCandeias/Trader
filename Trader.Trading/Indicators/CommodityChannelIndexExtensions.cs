using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class CommodityChannelIndexExtensions
{
    public static IEnumerable<decimal?> CommodityChannelIndex<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 20)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var selected = source.Select(selector);
        var ma = selected.SimpleMovingAverage(periods);
        var dev = selected.SimpleMovingAverageDeviation(periods);

        return selected.Zip(ma, dev).Select(x => (x.First - x.Second) / (0.015M * x.Third));
    }

    public static IEnumerable<decimal?> CommodityChannelIndex(this IEnumerable<Kline> source, int periods = 20)
    {
        return source.HLC3().CommodityChannelIndex(x => x, periods);
    }
}