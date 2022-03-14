namespace System.Collections.Generic;

public static class StdDevExtensions
{
    public static IEnumerable<decimal?> StandardDeviations(this IEnumerable<decimal?> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        foreach (var value in source.Variances(periods))
        {
            if (value.HasValue)
            {
                yield return (decimal?)Math.Sqrt((double)value.Value);
            }
            else
            {
                yield return null;
            }
        }
    }
}