using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class MovingSumExtensions
{
    /// <summary>
    /// Calculates the Moving Sum over the specified source.
    /// </summary>
    /// <param name="source">The source for moving sum calculation.</param>
    /// <param name="periods">The number of periods for moving sum calculation.</param>
    public static IEnumerable<decimal?> MovingSum<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var queue = QueuePool<decimal?>.Shared.Get();

        try
        {
            decimal? sum = null;

            foreach (var item in source)
            {
                var current = selector(item);

                if (current.HasValue)
                {
                    if (sum.HasValue)
                    {
                        sum += current;
                    }
                    else
                    {
                        sum = current;
                    }
                }

                queue.Enqueue(current);

                if (queue.Count > periods)
                {
                    var old = queue.Dequeue();
                    if (old.HasValue && sum.HasValue)
                    {
                        sum -= old;
                    }
                }

                yield return sum!;
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(queue);
        }
    }

    public static IEnumerable<decimal?> MovingSum(this IEnumerable<decimal?> source, int periods)
    {
        return source.MovingSum(x => x, periods);
    }
}