using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    public static class MovingSumExtensions
    {
        public static IEnumerable<decimal> MovingSum(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentNullException(nameof(periods));

            return MovingSumInner(items, periods);
        }

        private static IEnumerable<decimal> MovingSumInner(this IEnumerable<decimal> items, int periods)
        {
            var queue = new Queue<decimal>(periods);
            var sum = 0m;

            foreach (var item in items)
            {
                // make space for the new item
                if (queue.Count >= periods)
                {
                    sum -= queue.Dequeue();
                }

                // add the new item
                queue.Enqueue(item);
                sum += item;

                // return the moving sum
                yield return sum;
            }
        }
    }
}