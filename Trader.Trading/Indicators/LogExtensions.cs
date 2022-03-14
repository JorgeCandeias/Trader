namespace System.Collections.Generic;

public static class LogExtensions
{
    public static IEnumerable<decimal?> Log(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        foreach (var value in source)
        {
            if (value.HasValue)
            {
                yield return (decimal)Math.Log((double)value.Value);
            }
            else
            {
                yield return null;
            }
        }
    }
}