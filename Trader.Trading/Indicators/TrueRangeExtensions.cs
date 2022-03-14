namespace System.Collections.Generic;

public static class TrueRangeExtensions
{
    public static IEnumerable<decimal?> TrueRanges(this IEnumerable<(decimal? High, decimal? Low, decimal? Close)> source)
    {
        Guard.IsNotNull(source, nameof(source));

        var enumerator = source.GetEnumerator();

        // calculate the first period
        if (enumerator.MoveNext())
        {
            var value = enumerator.Current;
            var prev = value;

            yield return value.High - value.Low;

            // calculate the remaining periods
            while (enumerator.MoveNext())
            {
                value = enumerator.Current;

                var highLow = value.High - value.Low;
                var highClose = value.High.HasValue && prev.Close.HasValue ? (decimal?)Math.Abs(value.High.Value - prev.Close.Value) : null;
                var lowClose = value.Low.HasValue && prev.Close.HasValue ? (decimal?)Math.Abs(value.Low.Value - prev.Close.Value) : null;
                var trueRange = highLow.HasValue && highClose.HasValue && lowClose.HasValue ? (decimal?)Math.Max(highLow.Value, Math.Max(highClose.Value, lowClose.Value)) : null;

                yield return trueRange;

                prev = value;
            }
        }
    }

    public static IEnumerable<decimal?> TrueRanges<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector)
    {
        return source.Select(x => (highSelector(x), lowSelector(x), closeSelector(x))).TrueRanges();
    }

    public static IEnumerable<decimal?> TrueRanges(this IEnumerable<Kline> source)
    {
        return source.TrueRanges(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice);
    }
}