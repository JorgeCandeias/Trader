using System;
using System.Collections.Generic;

namespace Trader.Trading.Indicators
{
    public static class SimpleMovingAverageExtensions
    {
        public static IEnumerable<decimal> SimpleMovingAverage(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return SimpleMovingAverageInner(items, periods);
        }

        private static IEnumerable<decimal> SimpleMovingAverageInner(this IEnumerable<decimal> items, int periods)
        {
            var count = 0;

            foreach (var sum in items.MovingSum(periods))
            {
                count = Math.Min(++count, periods);

                yield return sum / count;
            }
        }
    }
}