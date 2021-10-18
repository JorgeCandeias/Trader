namespace System
{
    public static class ArrayExtensions
    {
        /// <inheritdoc cref="ArraySegment{T}.ArraySegment(T[], int, int)"/>
        public static ArraySegment<T> AsSegment<T>(this T[] array, int offset, int count)
        {
            return new ArraySegment<T>(array, offset, count);
        }
    }
}