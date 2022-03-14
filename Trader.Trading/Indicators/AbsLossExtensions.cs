namespace System.Collections.Generic;

public static class AbsLossExtensions
{
    /// <summary>
    /// Calculates the absolute loss between the current value and the previous value over the specified source.
    /// </summary>
    /// <param name="source">The source for absolute loss calculation.</param>
    public static IEnumerable<decimal?> AbsLoss(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));
        var enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            decimal? prev = enumerator.Current;

            yield return null;

            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;

                if (value.HasValue && prev.HasValue)
                {
                    yield return Math.Abs(Math.Min(value.Value - prev.Value, 0m));
                }
                else
                {
                    yield return null;
                }

                prev = value;
            }
        }
    }
}