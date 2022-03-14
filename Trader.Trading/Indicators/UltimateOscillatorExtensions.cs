using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Core.Pooling;

namespace Outcompute.Trader.Trading.Indicators;

public static class UltimateOscillatorExtensions
{
    public static IEnumerable<decimal?> UltimateOscillator(this IEnumerable<(decimal? Value, decimal? High, decimal? Low)> source, int fastLength = 7, int mediumLength = 14, int slowLength = 28)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(fastLength, 1, nameof(fastLength));
        Guard.IsGreaterThanOrEqualTo(mediumLength, 1, nameof(mediumLength));
        Guard.IsGreaterThanOrEqualTo(slowLength, 1, nameof(slowLength));

        var bpFastQueue = QueuePool<decimal?>.Shared.Get();
        var bpFastSum = 0M;

        var bpMediumQueue = QueuePool<decimal?>.Shared.Get();
        var bpMediumSum = 0M;

        var bpSlowQueue = QueuePool<decimal?>.Shared.Get();
        var bpSlowSum = 0M;

        var trFastQueue = QueuePool<decimal?>.Shared.Get();
        var trFastSum = 0M;

        var trMediumQueue = QueuePool<decimal?>.Shared.Get();
        var trMediumSum = 0M;

        var trSlowQueue = QueuePool<decimal?>.Shared.Get();
        var trSlowSum = 0M;

        try
        {
            foreach (var item in source.WithPrevious())
            {
                var rangeHigh = MathN.Max(item.Current.High, item.Previous.Value);
                var rangeLow = MathN.Min(item.Current.Low, item.Previous.Value);
                var buyingPressure = item.Current.Value - rangeLow;
                var trueRange = rangeHigh - rangeLow;

                bpFastSum += buyingPressure.GetValueOrDefault(0);
                bpFastQueue.Enqueue(buyingPressure);
                if (bpFastQueue.Count > fastLength)
                {
                    bpFastSum -= bpFastQueue.Dequeue().GetValueOrDefault(0);
                }

                bpMediumSum += buyingPressure.GetValueOrDefault(0);
                bpMediumQueue.Enqueue(buyingPressure);
                if (bpMediumQueue.Count > mediumLength)
                {
                    bpMediumSum -= bpMediumQueue.Dequeue().GetValueOrDefault(0);
                }

                bpSlowSum += buyingPressure.GetValueOrDefault(0);
                bpSlowQueue.Enqueue(buyingPressure);
                if (bpSlowQueue.Count > slowLength)
                {
                    bpSlowSum -= bpSlowQueue.Dequeue().GetValueOrDefault(0);
                }

                trFastSum += trueRange.GetValueOrDefault(0);
                trFastQueue.Enqueue(trueRange);
                if (trFastQueue.Count > fastLength)
                {
                    trFastSum -= trFastQueue.Dequeue().GetValueOrDefault(0);
                }

                trMediumSum += trueRange.GetValueOrDefault(0);
                trMediumQueue.Enqueue(trueRange);
                if (trMediumQueue.Count > mediumLength)
                {
                    trMediumSum -= trMediumQueue.Dequeue().GetValueOrDefault(0);
                }

                trSlowSum += trueRange.GetValueOrDefault(0);
                trSlowQueue.Enqueue(trueRange);
                if (trSlowQueue.Count > slowLength)
                {
                    trSlowSum -= trSlowQueue.Dequeue().GetValueOrDefault(0);
                }

                var fastAverage = MathN.SafeDiv(bpFastSum, trFastSum);
                var mediumAverage = MathN.SafeDiv(bpMediumSum, trMediumSum);
                var slowAverage = MathN.SafeDiv(bpSlowSum, trSlowSum);

                yield return 100 * (4 * fastAverage + 2 * mediumAverage + slowAverage) / 7;
            }
        }
        finally
        {
            QueuePool<decimal?>.Shared.Return(bpFastQueue);
            QueuePool<decimal?>.Shared.Return(bpMediumQueue);
            QueuePool<decimal?>.Shared.Return(bpSlowQueue);
            QueuePool<decimal?>.Shared.Return(trFastQueue);
            QueuePool<decimal?>.Shared.Return(trMediumQueue);
            QueuePool<decimal?>.Shared.Return(trSlowQueue);
        }
    }

    public static IEnumerable<decimal?> UltimateOscillator<T>(this IEnumerable<T> source, Func<T, decimal?> valueSelector, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, int fastLength = 7, int mediumLength = 14, int slowLength = 28)
    {
        return source.Select(x => (valueSelector(x), highSelector(x), lowSelector(x))).UltimateOscillator(fastLength, mediumLength, slowLength);
    }

    public static IEnumerable<decimal?> UltimateOscillator(this IEnumerable<Kline> source, int fastLength = 7, int mediumLength = 14, int slowLength = 28)
    {
        return source.UltimateOscillator(x => x.ClosePrice, x => x.HighPrice, x => x.LowPrice, fastLength, mediumLength, slowLength);
    }
}