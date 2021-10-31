using Outcompute.Trader.Trading.Indicators;
using System.Linq;

namespace System.Collections.Generic
{
    public static class SmaExtensions
    {
        /// <summary>
        /// Calculates the Simple Moving Average over the specified source.
        /// </summary>
        /// <param name="source">The source for SMA calculation.</param>
        /// <param name="periods">The number of periods for SMA calculation.</param>
        public static IEnumerable<decimal> Sma(this IEnumerable<decimal> source, int periods)
        {
            return new SmaIterator(source, periods);
        }

        /// <inheritdoc cref="Sma(IEnumerable{decimal}, int)"/>
        /// <param name="selector">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> Sma<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
        {
            var transformed = source.Select(selector);

            return transformed.Sma(periods);
        }

        public static decimal LastSma(this IEnumerable<decimal> source, int periods)
        {
            return source.Sma(periods).Last();
        }

        public static decimal LastSma<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
        {
            return source.Sma(selector, periods).Last();
        }
    }
}