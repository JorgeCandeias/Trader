using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class AverageDirectionalIndexExtensions
{
    public static IEnumerable<decimal?> AverageDirectionalIndex<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int adxLength = 14, int diLength = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));
        Guard.IsGreaterThanOrEqualTo(adxLength, 1, nameof(adxLength));
        Guard.IsGreaterThanOrEqualTo(diLength, 1, nameof(diLength));

        var up = source.Select(highSelector).Change();
        var down = source.Select(lowSelector).Change().Select(x => -x);
        var updown = up.Zip(down, (x, y) => (Up: x, Down: y)).ToList();

        var atr = source.AverageTrueRanges(highSelector, lowSelector, closeSelector, diLength, AtrMethod.Rma).ToList();

        var plus = updown
            .Select(x =>
            {
                if (x.Up.HasValue && x.Down.HasValue)
                {
                    return x.Up.Value > x.Down.Value && x.Up.Value > 0 ? x.Up : 0;
                }
                return null;
            })
            .RunningMovingAverage(diLength)
            .Zip(atr, (x, y) => 100 * x / y)
            .FillNullableGaps()
            .ToList();

        var minus = updown
            .Select(x =>
            {
                if (x.Up.HasValue && x.Down.HasValue)
                {
                    return x.Down.Value > x.Up.Value && x.Down.Value > 0 ? x.Down : 0;
                }
                return null;
            })
            .RunningMovingAverage(diLength)
            .Zip(atr, (x, y) => 100 * x / y)
            .FillNullableGaps()
            .ToList();

        var absDiff = plus.Zip(minus, (p, m) => p - m).Abs();
        var safeSum = plus.Zip(minus, (p, m) => p + m).Select(x => x == 0 ? 1 : x);

        return absDiff
            .Zip(safeSum, (d, s) => d / s)
            .RunningMovingAverage(adxLength)
            .Select(x => x * 100);
    }

    public static IEnumerable<decimal?> AverageDirectionalIndex(this IEnumerable<Kline> source, int adxLength = 14, int diLength = 14)
    {
        return source.AverageDirectionalIndex(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, adxLength, diLength);
    }
}