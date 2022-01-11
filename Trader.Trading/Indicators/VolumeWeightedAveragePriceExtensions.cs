using Outcompute.Trader.Core.Pooling;

namespace System.Collections.Generic;

public static class VolumeWeightedAveragePriceExtensions
{
    public enum KdjSide
    {
        None,
        Up,
        Down
    }

    public static IEnumerable<decimal> VolumeWeightedAveragePrice(this IEnumerable<Kline> source, int periods = 2000)
    {
        Guard.IsNotNull(source, nameof(source));

        var d1 = 0M;
        var d2 = 0M;

        var d1Queue = QueuePool<decimal>.Shared.Get();
        var d2Queue = QueuePool<decimal>.Shared.Get();

        foreach (var item in source)
        {
            if (d1Queue.Count >= periods)
            {
                d1 -= d1Queue.Dequeue();
                d2 -= d2Queue.Dequeue();
            }

            var price = (item.HighPrice + item.LowPrice + item.ClosePrice) / 3;
            var d1i = price * item.Volume;
            var d2i = item.Volume;

            d1 += d1i;
            d2 += d2i;

            d1Queue.Enqueue(d1i);
            d2Queue.Enqueue(d2i);

            yield return d1 / d2;
        }

        QueuePool<decimal>.Shared.Return(d1Queue);
        QueuePool<decimal>.Shared.Return(d2Queue);
    }
}