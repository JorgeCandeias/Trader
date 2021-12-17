namespace System.Collections.Generic;

public static class EmaExtensions
{
    /// <summary>
    /// Calculates the Exponential Moving Average over the specified source.
    /// </summary>
    /// <param name="source">The source for EMA calculation.</param>
    /// <param name="periods">The number of periods for EMA calculation.</param>
    public static IEnumerable<decimal> Ema(this IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));

        using var enumerator = source.GetEnumerator();

        var ema = 0M;
        var k = 2M / (periods + 1M);

        // handle the first value
        if (enumerator.MoveNext())
        {
            ema = enumerator.Current;
            yield return ema;

            // handle the remaining value
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;

                ema = (value * k) + (ema * (1 - k));
                yield return ema;
            }
        }
    }

    public static IEnumerable<decimal> Ema<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Ema(periods);
    }

    public static IEnumerable<decimal> Ema(this IEnumerable<Kline> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Select(x => x.ClosePrice).Ema(periods);
    }
}