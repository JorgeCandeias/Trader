using Outcompute.Trader.Core.Pooling;

namespace System.Collections.Generic;

internal static class VarianceExtensions
{
    public static IEnumerable<decimal> Variance(this IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        var window = QueuePool<decimal>.Shared.Get();
        var sum = 0.0M;
        var squares = 0.0M;

        foreach (var value in source)
        {
            if (window.Count >= periods)
            {
                var old = window.Dequeue();
                sum -= old;
                squares -= old * old;
            }

            window.Enqueue(value);
            sum += value;
            squares += value * value;

            var mean1 = sum / window.Count;
            var mean2 = squares / window.Count;

            yield return Math.Max(0, mean2 - mean1 * mean1);
        }

        QueuePool<decimal>.Shared.Return(window);
    }
}