using Outcompute.Trader.Core.Pooling;

namespace System.Collections.Generic;

internal static class MomentumExtensions
{
    public static IEnumerable<decimal?> Momentum(this IEnumerable<decimal?> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var enumerator = source.GetEnumerator();
        var queue = QueuePool<decimal?>.Shared.Get();

        try
        {
            // advance the first n periods
            for (var i = 0; i < periods; i++)
            {
                if (enumerator.MoveNext())
                {
                    queue.Enqueue(enumerator.Current);

                    yield return null;
                }
                else
                {
                    yield break;
                }
            }

            // return differences for the rest of the enumeration
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                var comparand = queue.Dequeue();

                yield return current - comparand;

                queue.Enqueue(current);
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(queue);
        }
    }

    public static IEnumerable<decimal?> Momentum(this IEnumerable<Kline> source, int periods)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .Momentum(periods);
    }
}