using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public record struct LowestHighestValue(decimal? Lowest, decimal? Highest)
{
    public static LowestHighestValue Empty { get; } = new LowestHighestValue();
}

public static class LowestHighestExtensions
{
    /// <summary>
    /// Yields both the lowest and highest value in <paramref name="source"/> within <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<LowestHighestValue> LowestHighest<T>(this IEnumerable<T> source, Func<T, decimal?> lowSelector, Func<T, decimal?> highSelector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var queue = QueuePool<(decimal? Low, decimal? High)>.Shared.Get();

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

                var current = enumerator.Current;
                var low = lowSelector(current);
                var high = highSelector(current);

                queue.Enqueue((low, high));

                yield return LowestHighestValue.Empty;
            }

            // yielding phase
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                var low = lowSelector(current);
                var high = highSelector(current);

                queue.Enqueue((low, high));

                decimal? lowest = null;
                decimal? highest = null;

                foreach (var (lowCandidate, highCandidate) in queue)
                {
                    lowest = MathN.Min(lowest, lowCandidate, MinMaxBehavior.NonNullWins);
                    highest = MathN.Max(highest, highCandidate, MinMaxBehavior.NonNullWins);
                }

                yield return new LowestHighestValue(lowest, highest);

                queue.Dequeue();
            }
        }
        finally
        {
            QueuePool<(decimal? Low, decimal? High)>.Shared.Return(queue);
        }
    }
}