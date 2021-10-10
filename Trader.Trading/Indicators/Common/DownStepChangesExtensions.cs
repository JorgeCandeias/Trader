using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators.Common
{
    public static class DownStepChangesExtensions
    {
        /// <summary>
        /// If the difference between each item and its preceeding item is negative then emits the difference, otherwise emits zero.
        /// Always emits zero for the first item.
        /// </summary>
        public static IEnumerable<decimal> DownStepChanges(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return items.DownStepChangesInner();
        }

        /// <summary>
        /// If the difference between each item and its preceeding item is negative then emits the difference, otherwise emits zero.
        /// Always emits null for the first item.
        /// </summary>
        public static IEnumerable<decimal?> DownStepChanges(this IEnumerable<decimal?> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return items.DownStepChangesInner();
        }

        /// <summary>
        /// Inner implementation for <see cref="DownStepChanges(IEnumerable{decimal})"/>.
        /// </summary>
        private static IEnumerable<decimal> DownStepChangesInner(this IEnumerable<decimal> items)
        {
            foreach (var item in items.StepChanges())
            {
                yield return Math.Min(item, 0m);
            }
        }

        /// <summary>
        /// Inner implementation for <see cref="DownStepChanges(IEnumerable{decimal?})"/>.
        /// </summary>
        private static IEnumerable<decimal?> DownStepChangesInner(this IEnumerable<decimal?> items)
        {
            foreach (var item in items.StepChanges())
            {
                yield return item.HasValue ? Math.Min(item.Value, 0m) : null;
            }
        }
    }
}