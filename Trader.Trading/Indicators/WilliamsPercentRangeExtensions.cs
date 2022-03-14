namespace Outcompute.Trader.Trading.Indicators;

public static class WilliamsPercentRangeExtensions
{
    public static IEnumerable<decimal?> WilliamsPercentRange<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int length = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        var max = source.Highest(highSelector, length);
        var min = source.Lowest(lowSelector, length);

        var numerator = source.Select(closeSelector).Zip(max, (x, y) => x - y);
        var denominator = max.Zip(min, (x, y) => x - y);

        return numerator.Zip(denominator, (x, y) => 100 * x / y);
    }

    public static IEnumerable<decimal?> WilliamsPercentRange(this IEnumerable<Kline> source, int length = 14)
    {
        return source.WilliamsPercentRange(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, length);
    }
}