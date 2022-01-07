using Outcompute.Trader.Core.Pooling;

namespace System.Collections.Generic;

internal static class StdDevExtensions
{
    public static IEnumerable<decimal> StdDev(this IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        var window = QueuePool<double>.Shared.Get();
        var sum = 0.0;
        var squares = 0.0;

        foreach (var value in source)
        {
            if (window.Count >= periods)
            {
                var old = window.Dequeue();
                sum -= old;
                squares -= old * old;
            }

            var v = (double)value;
            sum += v;
            squares += v * v;

            window.Enqueue(v);

            yield return (decimal)Math.Sqrt((squares - sum * sum / periods) / (periods - 1));
        }

        QueuePool<double>.Shared.Return(window);
    }
}