using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators.Common
{
    public static class UpStepChangesExtensions
    {
        /// <summary>
        /// If the difference between each item and its preceeding item is positive then emits the difference, otherwise emits zero.
        /// Always emit zero for the first item.
        /// </summary>
        public static IEnumerable<decimal> UpStepChanges(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return items.UpStepChangesInner();
        }

        /// <summary>
        /// If the difference between each item and its preceeding item is positive then emits the difference, otherwise emits zero.
        /// Always emit null for the first item.
        /// </summary>
        public static IEnumerable<decimal?> UpStepChanges(this IEnumerable<decimal?> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return items.UpStepChangesInner();
        }

        /// <summary>
        /// Inner implementation for <see cref="UpStepChanges(IEnumerable{decimal})"/>.
        /// </summary>
        private static IEnumerable<decimal> UpStepChangesInner(this IEnumerable<decimal> items)
        {
            foreach (var item in items.StepChanges())
            {
                yield return Math.Max(item, 0m);
            }
        }

        /// <summary>
        /// Inner implementation for <see cref="UpStepChanges(IEnumerable{decimal?})"/>.
        /// </summary>
        private static IEnumerable<decimal?> UpStepChangesInner(this IEnumerable<decimal?> items)
        {
            foreach (var item in items.StepChanges())
            {
                yield return item.HasValue ? Math.Max(item.Value, 0m) : null;
            }
        }
    }
}