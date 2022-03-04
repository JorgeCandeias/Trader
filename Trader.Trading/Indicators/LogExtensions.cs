namespace System.Collections.Generic;

public static class LogExtensions
{
    public static IEnumerable<decimal> Log(this IEnumerable<decimal> source)
    {
        Guard.IsNotNull(source, nameof(source));

        foreach (var value in source)
        {
            yield return (decimal)Math.Log((double)value);
        }
    }

    public static IEnumerable<decimal> Log<T>(this IEnumerable<T> source, Func<T, decimal> selector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        return source.Select(selector).Log();
    }
}