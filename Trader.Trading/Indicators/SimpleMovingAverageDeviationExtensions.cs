using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class SimpleMovingAverageDeviationExtensions
{
    /// <summary>
    /// Yields the difference between the <paramref name="source"/> series and its SMA.
    /// </summary>
    public static IEnumerable<decimal?> SimpleMovingAverageDeviation(this IEnumerable<decimal?> source, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var queue = QueuePool<decimal?>.Shared.Get();
        try
        {
            var mean = source.Sma(periods).GetEnumerator();
            var window = source.MovingWindow(periods).GetEnumerator();

            while (mean.MoveNext() && window.MoveNext())
            {
                var sum = window.Current.Sum(x => MathN.Abs(x - mean.Current));
                var dev = sum / periods;

                yield return dev;
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(queue);
        }
    }

    public static IEnumerable<decimal?> SimpleMovingAverageDeviation<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        return source.Select(selector).SimpleMovingAverageDeviation(periods);
    }

    public static IEnumerable<decimal?> SimpleMovingAverageDeviation(this IEnumerable<Kline> source, int periods = 1)
    {
        return source.SimpleMovingAverageDeviation(x => x.ClosePrice, periods);
    }
}