using Outcompute.Trader.Trading.Indicators;
using System.Linq;

namespace System.Collections.Generic
{
    public static class AbsExtensions
    {
        /// <summary>
        /// Calculates absolute values over the specified source.
        /// </summary>
        /// <param name="source">The source for absolute value calculation.</param>
        /// <returns>An enumerable that calculates absolute values over the specified source when enumerated.</returns>
        public static IEnumerable<decimal> Abs(this IEnumerable<decimal> source)
        {
            return new AbsIterator(source);
        }

        /// <inheritdoc cref="Abs(IEnumerable{decimal})"/>
        /// <param name="selector">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> Abs<T>(this IEnumerable<T> source, Func<T, decimal> selector)
        {
            var transformed = source.Select(selector);

            return new AbsIterator(transformed);
        }
    }
}