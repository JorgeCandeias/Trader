namespace System.Collections.Generic
{
    public static class DifferencesExtensions
    {
        /// <inheritdoc cref="DifferencesInner{T}(IEnumerable{T}, Func{T, decimal})"/>
        public static IEnumerable<decimal> Differences(this IEnumerable<decimal> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return DifferencesInner(items, x => x);
        }

        /// <inheritdoc cref="DifferencesInner{T}(IEnumerable{T}, Func{T, decimal})"/>
        public static IEnumerable<decimal> Differences<T>(this IEnumerable<T> items, Func<T, decimal> accessor)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            if (accessor is null) throw new ArgumentNullException(nameof(accessor));

            return DifferencesInner(items, accessor);
        }

        /// <summary>
        /// Emits the difference between each item and the preceeding item.
        /// Always emits zero for the first item.
        /// </summary>
        private static IEnumerable<decimal> DifferencesInner<T>(IEnumerable<T> items, Func<T, decimal> accessor)
        {
            var enumerator = items.GetEnumerator();

            // always return the first step change as zero
            if (enumerator.MoveNext())
            {
                yield return 0m;

                var last = accessor(enumerator.Current);

                // return the following steps changes as normal
                while (enumerator.MoveNext())
                {
                    var current = accessor(enumerator.Current);

                    yield return current - last;

                    last = current;
                }
            }
        }
    }
}