using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class MovingWindowExtensions
{
    /// <summary>
    /// Yields a moving window over <paramref name="source"/> of size <paramref name="length"/>.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> MovingWindow<T>(this IEnumerable<T> source, int length = 0)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(length, 0, nameof(length));

        var queue = QueuePool<T>.Shared.Get();

        foreach (var item in source)
        {
            queue.Enqueue(item);

            if (queue.Count > length)
            {
                queue.Dequeue();
            }

            yield return queue.ToList();
        }

        QueuePool<T>.Shared.Return(queue);
    }
}