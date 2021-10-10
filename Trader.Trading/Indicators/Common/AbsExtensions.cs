namespace System.Collections.Generic
{
    public static class AbsExtensions
    {
        /// <inheritdoc cref="AbsInner(IEnumerable{decimal})"/>
        public static IEnumerable<decimal> Abs(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return AbsInner(items, x => x);
        }

        /// <inheritdoc cref="AbsInner(IEnumerable{decimal})"/>
        public static IEnumerable<decimal> Abs<T>(this IEnumerable<T> items, Func<T, decimal> accessor)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));

            return AbsInner(items, accessor);
        }

        /// <summary>
        /// Emits the absolute value of each item in the given enumerable.
        /// </summary>
        private static IEnumerable<decimal> AbsInner<T>(IEnumerable<T> items, Func<T, decimal> accessor)
        {
            foreach (var item in items)
            {
                var value = accessor(item);

                yield return Math.Abs(value);
            }
        }
    }
}