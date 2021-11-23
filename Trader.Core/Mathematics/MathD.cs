namespace System
{
    /// <summary>
    /// Mathematical functions for decimal values.
    /// </summary>
    public static class MathD
    {
        /// <summary>
        /// Returns <paramref name="ratio"/> clamped to the inclusive range of [0.0, 1.0].
        /// </summary>
        /// <param name="ratio">The ratio to clamp.</param>
        /// <returns>The clamped ratio.</returns>
        public static decimal Clamp01(decimal ratio)
        {
            return Math.Clamp(ratio, 0M, 1M);
        }

        /// <summary>
        /// Performs clamped linear interpolation between the specified range.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <param name="ratio">A value between 0.0 and 1.0 to interpolate to range with.</param>
        /// <returns>The interpolated value from the range.</returns>
        public static decimal Lerp(decimal start, decimal end, decimal ratio)
        {
            ratio = Clamp01(ratio);

            return LerpUnclamped(start, end, ratio);
        }

        /// <summary>
        /// Performs unclamped linear interpolation between the specified range.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <param name="ratio">A value between 0.0 and 1.0 to interpolate to range with.</param>
        /// <returns>The interpolated value from the range.</returns>
        public static decimal LerpUnclamped(decimal start, decimal end, decimal ratio)
        {
            var range = end - start;
            var adjusted = ratio * range;
            var value = adjusted + start;

            return value;
        }

        /// <summary>
        /// Calculate the relative clamped linear interpolation ratio given the specified range and value.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <param name="value">The value from the value.</param>
        /// <returns>The interpolation rate between 0.0 and 1.0.</returns>
        public static decimal InverseLerp(decimal start, decimal end, decimal value)
        {
            var rate = InverseLerpUnclamped(start, end, value);

            rate = Clamp01(rate);

            return rate;
        }

        /// <summary>
        /// Calculate the relative unclamped linear interpolation ratio given the specified range and value.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <param name="value">The value from the value.</param>
        /// <returns>The interpolation rate between 0.0 and 1.0.</returns>
        public static decimal InverseLerpUnclamped(decimal start, decimal end, decimal value)
        {
            if (start == end)
            {
                return 0M;
            }

            var range = end - start;
            var adjusted = value - start;
            var rate = adjusted / range;

            return rate;
        }

        /// <summary>
        /// Converts a value from the source scale into a value of the target scale using clamped linear interpolation.
        /// </summary>
        /// <param name="sourceStart">The start of the source range.</param>
        /// <param name="sourceEnd">The end of the source range.</param>
        /// <param name="sourceValue">The value from the source range to convert.</param>
        /// <param name="targetStart">The start of the target range.</param>
        /// <param name="targetEnd">The end of the target range.</param>
        /// <returns>A value from target range.</returns>
        public static decimal LerpBetween(decimal sourceStart, decimal sourceEnd, decimal sourceValue, decimal targetStart, decimal targetEnd)
        {
            var ratio = InverseLerp(sourceStart, sourceEnd, sourceValue);
            var targetValue = Lerp(targetStart, targetEnd, ratio);

            return targetValue;
        }

        /// <summary>
        /// Converts a value from the source scale into a value of the target scale using unclamped linear interpolation.
        /// </summary>
        /// <param name="sourceStart">The start of the source range.</param>
        /// <param name="sourceEnd">The end of the source range.</param>
        /// <param name="sourceValue">The value from the source range to convert.</param>
        /// <param name="targetStart">The start of the target range.</param>
        /// <param name="targetEnd">The end of the target range.</param>
        /// <returns>A value from target range.</returns>
        public static decimal LerpBetweenUnclamped(decimal sourceStart, decimal sourceEnd, decimal sourceValue, decimal targetStart, decimal targetEnd)
        {
            var ratio = InverseLerpUnclamped(sourceStart, sourceEnd, sourceValue);
            var targetValue = LerpUnclamped(targetStart, targetEnd, ratio);

            return targetValue;
        }
    }
}