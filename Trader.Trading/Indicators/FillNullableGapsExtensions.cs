namespace Outcompute.Trader.Trading.Indicators;

public static class FillNullableGapsExtensions
{
    public static IEnumerable<T?> FillNullableGaps<T>(this IEnumerable<T?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        T? prev = default;

        foreach (var value in source)
        {
            if (value != null)
            {
                prev = value;
            }

            yield return prev;
        }
    }
}