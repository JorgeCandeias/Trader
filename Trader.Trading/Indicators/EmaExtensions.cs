namespace System.Collections.Generic;

public static class EmaExtensions
{
    public static IEnumerable<decimal?> ExponentialMovingAverage(this IEnumerable<decimal?> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        using var enumerator = source.GetEnumerator();

        decimal? ema = null;
        var k = 2M / (periods + 1M);

        // handle the first value
        if (enumerator.MoveNext())
        {
            ema = enumerator.Current;
            yield return ema;

            // handle the remaining value
            while (enumerator.MoveNext())
            {
                var next = enumerator.Current;

                if (ema.HasValue && next.HasValue)
                {
                    ema = (next * k) + (ema * (1 - k));
                }
                else
                {
                    ema = next;
                }

                yield return ema;
            }
        }
    }

    public static IEnumerable<decimal?> ExponentialMovingAverage(this IEnumerable<Kline> source, int periods)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .ExponentialMovingAverage(periods);
    }
}