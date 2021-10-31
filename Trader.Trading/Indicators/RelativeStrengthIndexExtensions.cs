namespace System.Collections.Generic
{
    public static class RelativeStrengthIndexExtensions
    {
        /// <inheritdoc cref="LastRelativeStrengthIndex{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static decimal LastRelativeStrengthIndex(this IEnumerable<decimal> items, int periods)
        {
            return items.LastRelativeStrengthIndex(x => x, periods);
        }

        /// <summary>
        /// Returns the RSI for the last position in the specified collection.
        /// Throws <see cref="InvalidOperationException"/> if the collection is empty.
        /// </summary>
        public static decimal LastRelativeStrengthIndex<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            decimal? last = null;

            foreach (var value in items.RelativeStrengthIndex(accessor, periods))
            {
                last = value;
            }

            if (last.HasValue)
            {
                return last.Value;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <inheritdoc cref="LastRelativeStrengthIndexOrDefault{T}(IEnumerable{T}, Func{T, decimal}, int, decimal)"/>
        public static decimal LastRelativeStrengthIndexOrDefault(this IEnumerable<decimal> items, int periods, decimal defaultValue = 50)
        {
            return items.LastRelativeStrengthIndexOrDefault(x => x, periods, defaultValue);
        }

        /// <summary>
        /// Returns the RSI for the last position in the specified collection.
        /// Returns <paramref name="defaultValue"/> if the collection is empty.
        /// </summary>
        public static decimal LastRelativeStrengthIndexOrDefault<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods, decimal defaultValue = 50)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            decimal? last = null;

            foreach (var value in items.RelativeStrengthIndex(accessor, periods))
            {
                last = value;
            }

            if (last.HasValue)
            {
                return last.Value;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <inheritdoc cref="RelativeStrengthIndexCore{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> RelativeStrengthIndex(this IEnumerable<decimal> items, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return RelativeStrengthIndexCore(items, x => x, periods);
        }

        /// <inheritdoc cref="RelativeStrengthIndexCore{T}(IEnumerable{T}, Func{T, decimal}, int)"/>
        public static IEnumerable<decimal> RelativeStrengthIndex<T>(this IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));
            if (periods < 1) throw new ArgumentOutOfRangeException(nameof(periods));

            return RelativeStrengthIndexCore(items, accessor, periods);
        }

        /// <summary>
        /// Yields the RSI over the specified periods for the specified data.
        /// Always returns 50 (inconclusive) for the first <paramref name="periods"/>-1 items.
        /// </summary>
        private static IEnumerable<decimal> RelativeStrengthIndexCore<T>(IEnumerable<T> items, Func<T, decimal> accessor, int periods)
        {
            var currentGains = items.Gain(accessor).GetEnumerator();
            var currentLosses = items.Loss(accessor).GetEnumerator();
            var avgGains = items.Gain(accessor).Rma(periods).GetEnumerator();
            var avgLosses = items.Loss(accessor).Abs().Rma(periods).GetEnumerator();
            var count = 0;

            while (currentGains.MoveNext() && currentLosses.MoveNext() && avgGains.MoveNext() && avgLosses.MoveNext())
            {
                // keep yield inconclusive until we have enough periods
                if (++count < periods)
                {
                    yield return 50;
                }

                // yield the first step of the rsi
                else
                {
                    // if there is no down in the market then return a full bullish rsi
                    if (avgLosses.Current is 0)
                    {
                        yield return 100m;
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
}