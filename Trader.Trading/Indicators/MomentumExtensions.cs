using Outcompute.Trader.Core.Pooling;

namespace System.Collections.Generic;

internal static class MomentumExtensions
{
    public static IEnumerable<decimal?> Momentum<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 10)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var queue = QueuePool<decimal?>.Shared.Get();

        try
        {
            var enumerator = source.GetEnumerator();

            // advance the first n periods
            for (var i = 0; i < periods; i++)
            {
                if (enumerator.MoveNext())
                {
                    queue.Enqueue(selector(enumerator.Current));

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
                var current = selector(enumerator.Current);
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

    public static IEnumerable<decimal?> Momentum(this IEnumerable<Kline> source, int periods = 10)
    {
        return source.Momentum(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> Momentum(this IEnumerable<decimal?> source, int periods = 10)
    {
        return source.Momentum(x => x, periods);
    }
}