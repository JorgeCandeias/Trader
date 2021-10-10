using Outcompute.Trader.Trading.Indicators.ObjectPools;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators.Common
{
    public static class MovingSumExtensions
    {
        /// <summary>
        /// Emits the moving sum of the last <paramref name="periods"/>.
        /// </summary>
        public static IEnumerable<decimal> MovingSum(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentNullException(nameof(periods));

            return items.MovingSumInner(periods);
        }

        private static IEnumerable<decimal> MovingSumInner(this IEnumerable<decimal> items, int periods)
        {
            var queue = QueuePool<decimal>.Shared.Get();
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

            QueuePool<decimal>.Shared.Return(queue);
        }
    }
}