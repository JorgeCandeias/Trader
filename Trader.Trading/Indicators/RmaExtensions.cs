namespace System.Collections.Generic;

public static class RmaExtensions
{
    public static IEnumerable<decimal?> RunningMovingAverage(this IEnumerable<decimal?> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var current = enumerator.Current;

            yield return current;

            while (enumerator.MoveNext())
            {
                var next = enumerator.Current;

                if (current.HasValue && next.HasValue)
                {
                    current = (((periods - 1) * current) + next) / periods;
                }
                else
                {
                    current = next;
                }

                yield return current;
            }
        }
    }

    public static IEnumerable<decimal?> RunningMovingAverage(this IEnumerable<Kline> source, int periods)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .RunningMovingAverage(periods);
    }
}