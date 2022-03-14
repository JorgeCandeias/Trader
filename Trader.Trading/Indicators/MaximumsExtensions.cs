namespace System.Collections.Generic;

public static class MaximumsExtensions
{
    /// <summary>
    /// Compares each source value with <paramref name="other"/> and yields the max of either.
    /// </summary>
    public static IEnumerable<decimal?> Maximums(this IEnumerable<decimal?> source, decimal? other)
    {
        Guard.IsNotNull(source, nameof(source));

        if (other.HasValue)
        {
            foreach (var value in source)
            {
                if (value.HasValue)
                {
                    yield return Math.Max(value.Value, other.Value);
                }
            }
        }
        else
        {
            foreach (var _ in source)
            {
                yield return null;
            }
        }
    }
}