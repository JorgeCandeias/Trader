namespace System.Collections.Generic;

public static class MaximumsExtensions
{
    /// <summary>
    /// Compares each source value with <paramref name="other"/> and yields the max of either.
    /// </summary>
    public static IEnumerable<decimal> Maximums(this IEnumerable<decimal> source, decimal other)
    {
        Guard.IsNotNull(source, nameof(source));

        foreach (var value in source)
        {
            yield return Math.Max(value, other);
        }
    }

    /// <inheritdoc cref="Maximums(IEnumerable{decimal}, decimal)"/>
    public static IEnumerable<decimal> Maximums<T>(this IEnumerable<T> source, Func<T, decimal> selector, decimal other)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Maximums(other);
    }
}