using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class LowestExtensions
{
    /// <summary>
    /// Yields the lowest value in <paramref name="source"/> within <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<decimal?> Lowest(this IEnumerable<decimal?> source, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var queue = QueuePool<decimal?>.Shared.Get();

        try
        {
            var enumerator = source.GetEnumerator();

            // seeding phase
            for (var i = 0; i < periods - 1; i++)
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }

                queue.Enqueue(enumerator.Current);

                yield return null;
            }

            // yielding phase
            while (enumerator.MoveNext())
            {
                queue.Enqueue(enumerator.Current);

                decimal? lowest = null;

                foreach (var candidate in queue)
                {
                    lowest = MathN.Min(lowest, candidate, MinMaxBehavior.NonNullWins);
                }

                yield return lowest!;

                queue.Dequeue();
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(queue);
        }
    }

    public static IEnumerable<decimal?> Lowest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        return source.Select(selector).Lowest(periods);
    }
}