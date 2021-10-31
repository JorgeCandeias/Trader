using Outcompute.Trader.Trading.Indicators;
using System.Linq;

namespace System.Collections.Generic
{
    public static class ChangeExtensions
    {
        /// <summary>
        /// Calculates the change between the current value and the previous value over the specified source.
        /// </summary>
        /// <param name="source">The source for change calculation.</param>
        public static IEnumerable<decimal> Change(this IEnumerable<decimal> source)
        {
            return new ChangeIterator(source);
        }

        /// <inheritdoc cref="Change(IEnumerable{decimal})"/>
        /// <param name="selector">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> Change<T>(this IEnumerable<T> source, Func<T, decimal> selector)
        {
            var transformed = source.Select(selector);

            return transformed.Change();
        }
    }
}