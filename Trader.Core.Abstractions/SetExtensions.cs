namespace System.Collections.Generic
{
    public static class SetExtensions
    {
        /// <summary>
        /// Adds the specified item to the set.
        /// If a matching item already exists then it is replaced.
        /// </summary>
        public static void Set<T>(this ISet<T> set, T item)
        {
            if (set is null) throw new ArgumentNullException(nameof(set));

            // remove the existing item by the same criteria of the set
            set.Remove(item);

            // add the new item - this should always work at this point
            if (!set.Add(item)) throw new Exception();
        }
    }
}