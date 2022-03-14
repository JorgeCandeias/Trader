using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class VolumeWeightedMovingAverageExtensions
{
    public static IEnumerable<decimal?> VolumeWeightedMovingAverage(this IEnumerable<(decimal? Close, decimal? Volume)> source, int periods = 20)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var numerator = source.Select(x => x.Close * x.Volume).SimpleMovingAverage(periods);
        var denominator = source.Select(x => x.Volume).SimpleMovingAverage(periods);

        return numerator.Zip(denominator, (x, y) => x / y);
    }

    public static IEnumerable<decimal?> VolumeWeightedMovingAverage<T>(this IEnumerable<T> source, Func<T, decimal?> closeSelector, Func<T, decimal?> volumeSelector, int periods = 20)
    {
        return source.Select(x => (closeSelector(x), volumeSelector(x))).VolumeWeightedMovingAverage(periods);
    }

    public static IEnumerable<decimal?> VolumeWeightedMovingAverage(this IEnumerable<Kline> source, int periods = 20)
    {
        return source.VolumeWeightedMovingAverage(x => x.ClosePrice, x => x.Volume, periods);
    }
}