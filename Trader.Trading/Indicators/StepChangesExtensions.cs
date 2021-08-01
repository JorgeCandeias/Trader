using System;
using System.Collections.Generic;

namespace Trader.Trading.Indicators
{
    public static class StepChangesExtensions
    {
        public static IEnumerable<decimal> StepChanges(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return StepChangesInner(items);
        }

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
    }
}