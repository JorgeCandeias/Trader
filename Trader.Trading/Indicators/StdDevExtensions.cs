namespace System.Collections.Generic;

internal static class StdDevExtensions
{
    public static IEnumerable<decimal> StdDev(this IEnumerable<decimal> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        foreach (var value in source.Variance(periods))
        {
            yield return (decimal)Math.Sqrt((double)value);
        }
    }
}