using Outcompute.Trader.Trading.Indicators;
using System.Linq;

namespace System.Collections.Generic
{
    public static class RsiExtensions
    {
        /// <summary>
        /// Calculates the RSI over the specified source.
        /// </summary>
        /// <param name="source">The source for RSI calculation.</param>
        /// <param name="periods">The number of periods for RSI calculation.</param>
        public static IEnumerable<decimal> Rsi(this IEnumerable<decimal> source, int periods)
        {
            return new RsiIterator(source, periods);
        }

        /// <inheritdoc cref="Rsi(IEnumerable{decimal}, int)"/>
        /// <param name="selector">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> Rsi<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
        {
            var transformed = source.Select(selector);

            return transformed.Rsi(periods);
        }

        public static decimal LastRsi(this IEnumerable<decimal> source, int periods)
        {
            return source.Rsi(periods).Last();
        }

        public static decimal LastRsi<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
        {
            return source.Rsi(selector, periods).Last();
        }
    }
}