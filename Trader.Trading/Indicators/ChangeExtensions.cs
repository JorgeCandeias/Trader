using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class ChangeExtensions
{
    /// <summary>
    /// Yields the difference between the current value and the previous value from <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<decimal?> Change<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var enumerator = source.GetEnumerator();
        var queue = QueuePool<decimal?>.Shared.Get();

        try
        {
            for (var i = 0; i < periods; i++)
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }

                queue.Enqueue(selector(enumerator.Current));

                yield return null;
            }

            while (enumerator.MoveNext())
            {
                var prev = queue.Dequeue();
                var current = selector(enumerator.Current);

                yield return current - prev;

                queue.Enqueue(current);
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(queue);
        }
    }

    /// <inheritdoc cref="Change{T}(IEnumerable{T}, Func{T, decimal?}, int)"/>
    public static IEnumerable<decimal?> Change(this IEnumerable<decimal?> source, int periods = 1)
    {
        return source.Change(x => x, periods);
    }

    /// <inheritdoc cref="Change{T}(IEnumerable{T}, Func{T, decimal?}, int)"/>
    public static IEnumerable<decimal?> Change(this IEnumerable<decimal> source, int periods = 1)
    {
        return source.Change(x => x, periods);
    }
}