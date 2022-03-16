namespace Outcompute.Trader.Trading.Indicators;

public record struct AverageDirectionalIndex(decimal? Adx, decimal? Plus, decimal? Minus);

public static class AverageDirectionalIndexExtensions
{
    public static IEnumerable<AverageDirectionalIndex> AverageDirectionalIndex<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int adxLength = 14, int diLength = 14)
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

        var atr = source.Atr(highSelector, lowSelector, closeSelector, diLength, AtrMethod.Rma).ToList();

        var plus = updown
            .Select(x =>
            {
                if (x.Up.HasValue && x.Down.HasValue)
                {
                    return x.Up.Value > x.Down.Value && x.Up.Value > 0 ? x.Up : 0;
                }
                return null;
            })
            .Rma(diLength)
            .Zip(atr, (x, y) => 100 * x / y)
            .FillNull()
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
            .Rma(diLength)
            .Zip(atr, (x, y) => 100 * x / y)
            .FillNull()
            .ToList();

        var absDiff = plus.Zip(minus, (p, m) => p - m).Abs();
        var safeSum = plus.Zip(minus, (p, m) => p + m).Select(x => x == 0 ? 1 : x);
        var adx = absDiff.Zip(safeSum, (d, s) => d / s).Rma(adxLength).Select(x => x * 100).ToList();

        return adx.Zip(plus, minus, (a, p, m) => new AverageDirectionalIndex(a, p, m));
    }

    public static IEnumerable<AverageDirectionalIndex> AverageDirectionalIndex(this IEnumerable<Kline> source, int adxLength = 14, int diLength = 14)
    {
        return source.AverageDirectionalIndex(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, adxLength, diLength);
    }
}