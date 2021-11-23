namespace System
{
    /// <summary>
    /// Mathematical functions for decimal values.
    /// </summary>
    public static class MathD
    {
        /// <summary>
        /// Performs linear interpolation between the specified range.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <param name="ratio">A value between 0.0 and 1.0 to interpolate to range with.</param>
        /// <returns>The interpolated value from the range.</returns>
        public static decimal Lerp(decimal start, decimal end, decimal ratio)
        {
            var range = end - start;
            var adjusted = ratio * range;
            var value = adjusted + start;

            return value;
        }

        /// <summary>
        /// Calculate the relative linear interpolation ratio given the specified range and value.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <param name="value">The value from the value.</param>
        /// <returns>The interpolation rate between 0.0 and 1.0.</returns>
        public static decimal InverseLerp(decimal start, decimal end, decimal value)
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
        /// Converts a value from the source scale into a value of the target scale using linear interpolation.
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
    }
}