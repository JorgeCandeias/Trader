using Outcompute.Trader.Core.Pooling;

namespace System.Collections.Generic;

internal static class MomentumExtensions
{
    public static IEnumerable<decimal> Momentum(this IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));

        var enumerator = source.GetEnumerator();
        var queue = QueuePool<decimal>.Shared.Get();

        // advance the first n periods
        for (var i = 0; i < periods; i++)
        {
            if (enumerator.MoveNext())
            {
                queue.Enqueue(enumerator.Current);
                yield return 0;
            }
            else
            {
                QueuePool<decimal>.Shared.Return(queue);
                yield break;
            }
        }

        // return differences for the rest of the enumeration
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;

            var comparand = queue.Dequeue();
            queue.Enqueue(current);

            yield return current - comparand;
        }

        QueuePool<decimal>.Shared.Return(queue);
    }

    public static IEnumerable<decimal> Momentum<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Momentum(periods);
    }
}