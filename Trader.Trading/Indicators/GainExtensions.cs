using Outcompute.Trader.Trading.Indicators;
using System.Linq;

namespace System.Collections.Generic
{
    public static class GainExtensions
    {
        /// <summary>
        /// Calculates the gain between the current value and the previous value over the specified source.
        /// Evaluates to zero if there is no gain.
        /// </summary>
        /// <param name="source">The source for gain calculation.</param>
        public static IEnumerable<decimal> Gain(this IEnumerable<decimal> source)
        {
            return new GainIterator(source);
        }

        /// <inheritdoc cref="Gain(IEnumerable{decimal})"/>
        /// <param name="selector">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> Gain<T>(this IEnumerable<T> source, Func<T, decimal> selector)
        {
            var transformed = source.Select(selector);

            return transformed.Gain();
        }
    }
}