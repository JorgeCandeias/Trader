using Outcompute.Trader.Core.Pooling;

namespace System.Collections.Generic;

public static class RateOfChangeExtensions
{
    public static IEnumerable<decimal> RateOfChange(this IEnumerable<decimal> source, int length = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(length, 0, nameof(length));

        var queue = QueuePool<decimal>.Shared.Get();
        var enumerator = source.GetEnumerator();

        // enumerate the first items to seed the indicator
        while (queue.Count < length && enumerator.MoveNext())
        {
            queue.Enqueue(enumerator.Current);
            yield return 0;
        }

        // enumerate the remaining items to produce values
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            var previous = queue.Dequeue();

            yield return (current - previous) / previous;

            queue.Enqueue(current);
        }

        QueuePool<decimal>.Shared.Return(queue);
    }

    public static IEnumerable<decimal> RateOfChange(this IEnumerable<Kline> source, int length = 9)
    {
        return source.RateOfChange(x => x.ClosePrice, length);
    }

    public static IEnumerable<decimal> RateOfChange<T>(this IEnumerable<T> source, Func<T, decimal> selector, int length = 9)
    {
        return source.Select(selector).RateOfChange(length);
    }
}