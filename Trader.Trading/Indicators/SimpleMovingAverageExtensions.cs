using Outcompute.Trader.Trading.Indicators.Common;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    public static class SimpleMovingAverageExtensions
    {
        /// <summary>
        /// Emits the simple moving average of the last <paramref name="periods"/>.
        /// Emits zero for the first <paramref name="periods"/>-1 items.
        /// </summary>
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
                if (++count < periods)
                {
                    yield return 0m;
                }

                yield return sum / periods;
            }
        }
    }
}