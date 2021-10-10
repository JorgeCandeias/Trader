namespace System.Collections.Generic
{
    public static class RelativeStrengthIndexExtensions
    {
        /// <inheritdoc cref="RelativeStrengthIndexInner{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> RelativeStrengthIndex(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return RelativeStrengthIndexInner(items, x => x, periods);
        }

        /// <inheritdoc cref="RelativeStrengthIndexInner{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> RelativeStrengthIndex<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return RelativeStrengthIndexInner(items, accessor, periods);
        }

        /// <summary>
        /// Yields the RSI over the specified periods for the specified data.
        /// Always returns 50 (inconclusive) for the first <paramref name="periods"/>-1 items.
        /// </summary>
        private static IEnumerable<decimal> RelativeStrengthIndexInner<T>(IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            var avgGains = items.Gains(accessor).SimpleMovingAverages(periods).GetEnumerator();
            var avgLosses = items.Losses(accessor).Abs().SimpleMovingAverages(periods).GetEnumerator();
            var count = 0;

            while (avgGains.MoveNext() && avgLosses.MoveNext())
            {
                // keep yield inconclusive until we have enough periods
                if (++count < periods)
                {
                    yield return 50;
                    continue;
                }

                // if there is no down in the market then return a full bullish rsi
                if (avgLosses.Current is 0)
                {
                    yield return 100;
                }
                else
                {
                    // otherwise calculate the rsi as normal
                    var relativeStrength = avgGains.Current / avgLosses.Current;
                    var relativeStrengthIndex = 100m - (100m / (1m + relativeStrength));

                    yield return relativeStrengthIndex;
                }
            }
        }
    }
}