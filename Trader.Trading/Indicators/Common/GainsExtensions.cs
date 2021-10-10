namespace System.Collections.Generic
{
    public static class GainsExtensions
    {
        /// <inheritdoc cref="GainsInner{T}(IEnumerable{T}, Func{T, decimal})"/>
        public static IEnumerable<decimal> Gains(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return GainsInner(items, x => x);
        }

        /// <inheritdoc cref="GainsInner{T}(IEnumerable{T}, Func{T, decimal})"/>
        public static IEnumerable<decimal> Gains<T>(this IEnumerable<T> items, Func<T, decimal> accessor)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return GainsInner(items, accessor);
        }

        /// <summary>
        /// If the difference between each item and its preceeding item is positive then emits the difference, otherwise emits zero.
        /// Always emit zero for the first item.
        /// </summary>
        private static IEnumerable<decimal> GainsInner<T>(IEnumerable<T> items, Func<T, decimal> accessor)
        {
            foreach (var item in items.Differences(accessor))
            {
                yield return Math.Max(item, 0m);
            }
        }
    }
}