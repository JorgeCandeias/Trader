using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators.Common
{
    public static class AbsExtensions
    {
        /// <summary>
        /// Emits the absolute value of each item in the given enumerable.
        /// </summary>
        public static IEnumerable<decimal> Abs(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return AbsInner(items);
        }

        /// <summary>
        /// Emits the absolute value of each item in the given enumerable.
        /// </summary>
        public static IEnumerable<decimal?> Abs(this IEnumerable<decimal?> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return AbsInner(items);
        }

        /// <summary>
        /// Inner implementation for <see cref="Abs(IEnumerable{decimal})"/>.
        /// </summary>
        private static IEnumerable<decimal> AbsInner(IEnumerable<decimal> items)
        {
            foreach (var item in items)
            {
                yield return Math.Abs(item);
            }
        }

        /// <summary>
        /// Inner implementation for <see cref="Abs(IEnumerable{decimal?})"/>.
        /// </summary>
        private static IEnumerable<decimal?> AbsInner(IEnumerable<decimal?> items)
        {
            foreach (var item in items)
            {
                yield return item is null ? null : Math.Abs(item.Value);
            }
        }
    }
}