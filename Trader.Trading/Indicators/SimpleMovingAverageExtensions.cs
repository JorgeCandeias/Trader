using Outcompute.Trader.Trading.Indicators.Common;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    public static class SimpleMovingAverageExtensions
    {
        /// <inheritdoc cref="SimpleMovingAveragesInner{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> SimpleMovingAverages(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return SimpleMovingAveragesInner(items, x => x, periods);
        }

        /// <inheritdoc cref="SimpleMovingAveragesInner{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> SimpleMovingAverages<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return SimpleMovingAveragesInner(items, accessor, periods);
        }

        /// <inheritdoc cref="LastSimpleMovingAverage{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static decimal LastSimpleMovingAverage(this IEnumerable<decimal> items, int periods)
        {
            return LastSimpleMovingAverage(items, x => x, periods);
        }

        /// <summary>
        /// Return the last simple moving average for the specified periods over the specified items.
        /// Returns zero if no simple moving average can be calculated for the specified periods.
        /// </summary>
        public static decimal LastSimpleMovingAverage<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            var last = 0m;

            foreach (var item in SimpleMovingAveragesInner(items, accessor, periods))
            {
                last = item;
            }

            return last;
        }

        /// <summary>
        /// For each value, yields the simple moving average of the last <paramref name="periods"/> values.
        /// Always yields zero for the first <paramref name="periods"/>-1 values.
        /// </summary>
        private static IEnumerable<decimal> SimpleMovingAveragesInner<T>(IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            var count = 0;

            foreach (var sum in items.MovingSums(accessor, periods))
            {
                // if we do not yet have enough periods then return zero
                if (++count < periods)
                {
                    yield return 0m;
                }

                // return the average over the periods
                yield return sum / periods;
            }
        }
    }
}