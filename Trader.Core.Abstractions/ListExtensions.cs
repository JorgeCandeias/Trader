using System.Collections.Immutable;

namespace System.Collections.Generic
{
    public static class ListExtensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            _ = list ?? throw new ArgumentNullException(nameof(list));
            _ = items ?? throw new ArgumentNullException(nameof(items));

            if (list is List<T> generic)
            {
                generic.AddRange(items);
                return;
            }

            if (list is ImmutableList<T>)
            {
                throw new InvalidOperationException();
            }

            foreach (var item in items)
            {
                list.Add(item);
            }
        }

        public static void ReplaceWith<T>(this IList<T> list, IEnumerable<T> items)
        {
            _ = list ?? throw new ArgumentNullException(nameof(list));
            _ = items ?? throw new ArgumentNullException(nameof(items));

            if (list is ImmutableList<T>)
            {
                throw new InvalidOperationException();
            }

            list.Clear();
            list.AddRange(items);
        }
    }
}