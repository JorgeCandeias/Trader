using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators
{
    public static class AbsoluteExtensions
    {
        public static IEnumerable<decimal> Absolute(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return AbsoluteInner(items);
        }

        private static IEnumerable<decimal> AbsoluteInner(IEnumerable<decimal> items)
        {
            foreach (var item in items)
            {
                yield return Math.Abs(item);
            }
        }
    }
}