namespace System.Collections.Generic;

public static class GainExtensions
{
    /// <summary>
    /// Calculates the gain between the current value and the previous value over the specified source.
    /// </summary>
    /// <param name="source">The source for gain calculation.</param>
    public static IEnumerable<decimal?> Gain(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        var enumerator = source.GetEnumerator();

        if (enumerator.MoveNext())
        {
            var prev = enumerator.Current;

            yield return null;

            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;

                if (value.HasValue && prev.HasValue)
                {
                    yield return Math.Max(value.Value - prev.Value, 0m);
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