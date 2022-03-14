namespace System.Collections.Generic;

public static class HL2Extensions
{
    public static IEnumerable<decimal?> HL2(this IEnumerable<(decimal? high, decimal? low)> source)
    {
        Guard.IsNotNull(source, nameof(source));

        foreach (var (high, low) in source)
        {
            yield return (high + low) / 2M;
        }
    }
}