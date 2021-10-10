namespace System.Collections.Generic
{
    public static class LossesExtensions
    {
        /// <inheritdoc cref="LossesInner{T}(IEnumerable{T}, Func{T, decimal})"/>
        public static IEnumerable<decimal> Losses(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return LossesInner(items, x => x);
        }

        /// <inheritdoc cref="LossesInner{T}(IEnumerable{T}, Func{T, decimal})"/>
        public static IEnumerable<decimal> Losses<T>(this IEnumerable<T> items, Func<T, decimal> accessor)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));

            return LossesInner(items, accessor);
        }

        /// <summary>
        /// If the difference between each item and its preceeding item is negative then emits the difference, otherwise emits zero.
        /// Always emits zero for the first item.
        /// </summary>
        private static IEnumerable<decimal> LossesInner<T>(IEnumerable<T> items, Func<T, decimal> accessor)
        {
            foreach (var item in items.Differences(accessor))
            {
                yield return Math.Min(item, 0m);
            }
        }
    }
}