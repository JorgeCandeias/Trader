namespace System.Collections.Generic;

public static class OppositesExtensions
{
    /// <summary>
    /// Yields the opposite of each value in <paramref name="source"/>.
    /// </summary>
    public static IEnumerable<decimal> Opposites(this IEnumerable<decimal> source)
    {
        Guard.IsNotNull(source, nameof(source));

        foreach (var value in source)
        {
            yield return -value;
        }
    }

    /// <inheritdoc cref="Opposites(IEnumerable{decimal})"/>
    public static IEnumerable<decimal> Opposites<T>(this IEnumerable<T> source, Func<T, decimal> selector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Opposites();
    }
}