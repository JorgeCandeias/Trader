using Outcompute.Trader.Trading.Indicators;
using System.Linq;

namespace System.Collections.Generic
{
    public static class MovingSumExtensions
    {
        /// <summary>
        /// Calculates the Moving Sum over the specified source.
        /// </summary>
        /// <param name="source">The source for moving sum calculation.</param>
        /// <param name="periods">The number of periods for moving sum calculation.</param>
        public static IEnumerable<decimal> MovingSum(this IEnumerable<decimal> source, int periods)
        {
            return new MovingSumIterator(source, periods);
        }

        /// <inheritdoc cref="MovingSum(IEnumerable{decimal}, int)"/>
        /// <param name="selector">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> MovingSum<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
        {
            var transformed = source.Select(selector);

            return transformed.MovingSum(periods);
        }
    }
}