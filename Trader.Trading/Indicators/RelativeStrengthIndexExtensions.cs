using Outcompute.Trader.Trading.Indicators.Common;
using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    public static class RelativeStrengthIndexExtensions
    {
        public static IEnumerable<decimal> RelativeStrengthIndex(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return RelativeStrengthIndexInner(items, periods);
        }

        private static IEnumerable<decimal> RelativeStrengthIndexInner(IEnumerable<decimal> items, int periods)
        {
            var averageUpEnumerator = items.UpStepChanges().SimpleMovingAverage(periods).GetEnumerator();
            var averageDownEnumerator = items.DownStepChanges().Abs().SimpleMovingAverage(periods).GetEnumerator();

            while (averageUpEnumerator.MoveNext() && averageDownEnumerator.MoveNext())
            {
                // if there is no down in the market then return a full bullish rsi
                if (averageDownEnumerator.Current is 0)
                {
                    yield return 100;
                }
                else
                {
                    // otherwise calculate the rsi as normal
                    var relativeStrength = averageUpEnumerator.Current / averageDownEnumerator.Current;
                    var relativeStrengthIndex = 100m - 100m / (1m + relativeStrength);

                    yield return relativeStrengthIndex;
                }
            }
        }
    }
}