using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    public static class UpStepChangesExtensions
    {
        public static IEnumerable<decimal> UpStepChanges(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return UpStepChangesInner(items);
        }

        private static IEnumerable<decimal> UpStepChangesInner(this IEnumerable<decimal> items)
        {
            foreach (var item in items.StepChanges())
            {
                yield return Math.Max(item, 0m);
            }
        }
    }
}