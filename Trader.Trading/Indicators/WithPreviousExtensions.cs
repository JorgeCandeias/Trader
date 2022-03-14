using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class WithPreviousExtensions
{
    public record struct WithPreviousValue<T>(T Current, T? Previous);

    public static IEnumerable<WithPreviousValue<T>> WithPrevious<T>(this IEnumerable<T> source, int length = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        var queue = QueuePool<T>.Shared.Get();

        try
        {
            var enumerator = source.GetEnumerator();

            for (var i = 0; i < length; i++)
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }

                var current = enumerator.Current;

                yield return new WithPreviousValue<T>(current, default);

                queue.Enqueue(current);
            }

            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                var prev = queue.Dequeue();

                yield return new WithPreviousValue<T>(current, prev);

                queue.Enqueue(current);
            }
        }
        finally
        {
            QueuePool<T>.Shared.Return(queue);
        }
    }
}