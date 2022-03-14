namespace System.Collections.Generic;

public static class SmaExtensions
{
    public static IEnumerable<decimal?> SimpleMovingAverage(this IEnumerable<decimal?> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var enumerator = source.GetEnumerator();
        var queue = new Queue<decimal?>(periods);

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

            var sum = 0M;
            var count = 0;

            foreach (var item in queue)
            {
                if (item.HasValue)
                {
                    sum += item.Value;
                    count++;
                }
            }

            if (count > 0)
            {
                yield return sum / count;
            }
            else
            {
                yield return null;
            }

            queue.Dequeue();
        }
    }

    public static IEnumerable<decimal?> SimpleMovingAverage(this IEnumerable<Kline> source, int periods)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .SimpleMovingAverage(periods);
    }
}