using Outcompute.Trader.Trading.Indicators;
using System.Linq;

namespace System.Collections.Generic
{
    public static class LossExtensions
    {
        /// <summary>
        /// Calculates the loss between the current value and the previous value over the specified source.
        /// Evaluates to zero if there is no loss.
        /// </summary>
        /// <param name="source">The source for loss calculation.</param>
        public static IEnumerable<decimal> Loss(this IEnumerable<decimal> source)
        {
            return new LossIterator(source);
        }

        /// <inheritdoc cref="Loss(IEnumerable{decimal})"/>
        /// <param name="selector">A transform function to apply to each element.</param>
        public static IEnumerable<decimal> Loss<T>(this IEnumerable<T> source, Func<T, decimal> selector)
        {
            var transformed = source.Select(selector);

            return transformed.Loss();
        }
    }
}