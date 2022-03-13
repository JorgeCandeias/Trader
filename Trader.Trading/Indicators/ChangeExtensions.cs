namespace System.Collections.Generic;

public static class ChangeExtensions
{
    public static IEnumerable<decimal?> Change(this IEnumerable<decimal> source, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        return source.Select(x => (decimal?)x).Change(periods);
    }

    public static IEnumerable<decimal?> Change(this IEnumerable<decimal?> source, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var enumerator = source.GetEnumerator();
        var queue = new Queue<decimal?>();

        for (var i = 0; i < periods; i++)
        {
            if (!enumerator.MoveNext())
            {
                yield break;
            }

            queue.Enqueue(enumerator.Current);
        }

        while (enumerator.MoveNext())
        {
            var prev = queue.Dequeue();
            var current = enumerator.Current;

            yield return current - prev;

            queue.Enqueue(current);
        }
    }
}