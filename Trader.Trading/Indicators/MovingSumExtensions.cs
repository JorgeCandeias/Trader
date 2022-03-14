using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class MovingSumExtensions
{
    /// <summary>
    /// Calculates the Moving Sum over the specified source.
    /// </summary>
    /// <param name="source">The source for moving sum calculation.</param>
    /// <param name="periods">The number of periods for moving sum calculation.</param>
    public static IEnumerable<decimal?> MovingSum(this IEnumerable<decimal?> source, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var queue = QueuePool<decimal?>.Shared.Get();

        try
        {
            var enumerator = source.GetEnumerator();
            var sum = 0M;

            // seeding phase
            for (var i = 0; i < periods; i++)
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }

                var current = enumerator.Current;

                sum += current.GetValueOrDefault(0);
                queue.Enqueue(enumerator.Current);

                yield return null;
            }

            // yielding phase
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;

                sum += current.GetValueOrDefault(0);
                queue.Enqueue(current);
                sum -= queue.Dequeue().GetValueOrDefault(0);
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(queue);
        }
    }

    public static IEnumerable<decimal?> MovingSum<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods)
    {
        return source.Select(selector).MovingSum(periods);
    }
}