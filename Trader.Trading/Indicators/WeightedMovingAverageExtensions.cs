namespace System.Collections.Generic;

public static class WeightedMovingAverageExtensions
{
    public static IEnumerable<decimal?> WeightedMovingAverage(this IEnumerable<Kline> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        return source.WeightedMovingAverage(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> WeightedMovingAverage(this IEnumerable<decimal> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        return source.WeightedMovingAverage(x => x, periods);
    }

    public static IEnumerable<decimal?> WeightedMovingAverage<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        return source.Select(selector).WeightedMovingAverage(periods);
    }

    public static IEnumerable<decimal?> WeightedMovingAverage(this IEnumerable<decimal?> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var queue = new Queue<decimal?>();
        var enumerator = source.GetEnumerator();

        for (var i = 0; i < periods - 1; i++)
        {
            if (!enumerator.MoveNext())
            {
                yield break;
            }

            queue.Enqueue(enumerator.Current);

            yield return null;
        }

        while (enumerator.MoveNext())
        {
            queue.Enqueue(enumerator.Current);

            var i = 0;
            var norm = 0M;
            decimal? sum = 0M;

            foreach (var item in queue)
            {
                var weight = ++i * periods;
                norm += weight;
                sum += item * weight;
            }

            queue.Dequeue();

            yield return sum / norm;
        }
    }
}