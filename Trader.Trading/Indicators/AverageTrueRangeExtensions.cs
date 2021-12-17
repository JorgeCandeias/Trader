namespace System.Collections.Generic;

internal static class AverageTrueRangeExtensions
{
    public static IEnumerable<decimal> AverageTrueRange(this IEnumerable<Kline> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        var enumerator = source.TrueRange().GetEnumerator();

        // calculate the first period
        if (enumerator.MoveNext())
        {
            var prev = enumerator.Current;
            yield return prev;

            // calculate the remaining periods
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                var atr = ((prev * (periods - 1)) + value) / periods;

                yield return atr;

                prev = atr;
            }
        }
    }
}