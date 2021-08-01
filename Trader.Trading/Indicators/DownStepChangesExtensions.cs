using System;
using System.Collections.Generic;

namespace Trader.Trading.Indicators
{
    public static class DownStepChangesExtensions
    {
        public static IEnumerable<decimal> DownStepChanges(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return DownStepChangesInner(items);
        }

        private static IEnumerable<decimal> DownStepChangesInner(this IEnumerable<decimal> items)
        {
            foreach (var item in items.StepChanges())
            {
                yield return Math.Min(item, 0m);
            }
        }
    }
}