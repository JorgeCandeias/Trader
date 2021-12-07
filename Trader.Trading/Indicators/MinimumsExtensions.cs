namespace System.Collections.Generic;

public static class MinimumsExtensions
{
    /// <summary>
    /// Compares each source value with <paramref name="other"/> and yields the min of either.
    /// </summary>
    public static IEnumerable<decimal> Minimums(this IEnumerable<decimal> source, decimal other)
    {
        Guard.IsNotNull(source, nameof(source));

        foreach (var value in source)
        {
            yield return Math.Min(value, other);
        }
    }

    /// <inheritdoc cref="Minimums(IEnumerable{decimal}, decimal)"/>
    public static IEnumerable<decimal> Minimums<T>(this IEnumerable<T> source, Func<T, decimal> selector, decimal other)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Minimums(other);
    }
}