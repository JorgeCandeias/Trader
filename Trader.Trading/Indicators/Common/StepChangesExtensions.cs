using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators.Common
{
    public static class StepChangesExtensions
    {
        /// <summary>
        /// Emits the difference between each item and the preceeding item.
        /// Always emits zero for the first item.
        /// </summary>
        public static IEnumerable<decimal> StepChanges(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return items.StepChangesInner();
        }

        /// <summary>
        /// Emits the difference between each item and the preceeding item.
        /// Always emits null for the first item.
        /// </summary>
        public static IEnumerable<decimal?> StepChanges(this IEnumerable<decimal?> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return items.StepChangesInner();
        }

        /// <summary>
        /// Inner implementation for <see cref="StepChanges(IEnumerable{decimal})"/>.
        /// </summary>
        private static IEnumerable<decimal> StepChangesInner(this IEnumerable<decimal> items)
        {
            var enumerator = items.GetEnumerator();

            // always return the first step change as zero
            if (enumerator.MoveNext())
            {
                yield return 0m;

                var last = enumerator.Current;

                // return the following steps changes as normal
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current - last;

                    last = enumerator.Current;
                }
            }
        }

        /// <summary>
        /// Inner implementation for <see cref="StepChanges(IEnumerable{decimal?})"/>.
        /// </summary>
        private static IEnumerable<decimal?> StepChangesInner(this IEnumerable<decimal?> items)
        {
            var enumerator = items.GetEnumerator();

            // always return the first step change as null
            if (enumerator.MoveNext())
            {
                yield return null;

                var last = enumerator.Current;

                // return the following steps changes as normal
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current - last;

                    last = enumerator.Current;
                }
            }
        }
    }
}