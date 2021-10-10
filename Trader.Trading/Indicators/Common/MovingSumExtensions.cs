using Outcompute.Trader.Trading.Indicators.ObjectPools;

namespace System.Collections.Generic
{
    public static class MovingSumExtensions
    {
        /// <inheritdoc cref="MovingSumsInner{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> MovingSums(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentNullException(nameof(periods));

            return MovingSumsInner(items, x => x, periods);
        }

        /// <inheritdoc cref="MovingSumsInner{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> MovingSums<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentNullException(nameof(periods));

            return MovingSumsInner(items, accessor, periods);
        }

        /// <inheritdoc cref="LastMovingSum{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static decimal LastMovingSum(this IEnumerable<decimal> items, int periods)
        {
            return LastMovingSum(items, x => x, periods);
        }

        /// <summary>
        /// Returns the last moving sum over the specified periods for the specified items.
        /// Returns zero if no moving sum for the specified periods can be calculated.
        /// </summary>
        public static decimal LastMovingSum<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentNullException(nameof(periods));

            var last = 0m;

            foreach (var item in MovingSumsInner(items, accessor, periods))
            {
                last = item;
            }

            return last;
        }

        /// <summary>
        /// Yields the moving sum of the last <paramref name="periods"/> items.
        /// Always yields zero for the first <paramref name="periods"/>-1 items.
        /// </summary>
        private static IEnumerable<decimal> MovingSumsInner<T>(IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            var queue = QueuePool<decimal>.Shared.Get();
            var sum = 0m;

            foreach (var item in items)
            {
                // get the underlying value
                var value = accessor(item);

                // make space for the new item
                if (queue.Count >= periods)
                {
                    sum -= queue.Dequeue();
                }

                // add the new value
                queue.Enqueue(value);
                sum += value;

                // return the moving sum only if we have enough periods
                yield return queue.Count < periods ? 0m : sum;
            }

            QueuePool<decimal>.Shared.Return(queue);
        }
    }
}