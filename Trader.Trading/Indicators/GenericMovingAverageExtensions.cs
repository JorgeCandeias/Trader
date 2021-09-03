using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    public static class GenericMovingAverageExtensions
    {
        public static IEnumerable<decimal> GenericMovingAverage(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentNullException(nameof(periods));

            return GenericMovingAverageInner(items, periods);
        }

        private static IEnumerable<decimal> GenericMovingAverageInner(IEnumerable<decimal> items, int periods)
        {
            var queue = new Queue<decimal>(periods);
            var total = 0m;

            foreach (var item in items)
            {
                if (queue.Count >= periods)
                {
                    total -= queue.Dequeue();
                }

                queue.Enqueue(item);
                total += item;

                yield return total / queue.Count;
            }
        }
    }
}