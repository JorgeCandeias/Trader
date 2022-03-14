using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class HighestExtensions
{
    /// <summary>
    /// Yields the highest value in <paramref name="source"/> within <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<decimal?> Highest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
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

                queue.Enqueue(selector(enumerator.Current));

                yield return null;
            }

            // yielding phase
            while (enumerator.MoveNext())
            {
                queue.Enqueue(selector(enumerator.Current));

                decimal? highest = null;

                foreach (var candidate in queue)
                {
                    highest = MathN.Max(highest, candidate, MinMaxBehavior.NonNullWins);
                }

                yield return highest!;

                queue.Dequeue();
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(queue);
        }
    }

    public static IEnumerable<decimal?> Highest(this IEnumerable<Kline> source, int periods = 1)
    {
        return source.Highest(x => x.HighPrice, periods);
    }
}