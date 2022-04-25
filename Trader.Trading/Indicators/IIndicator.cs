namespace Outcompute.Trader.Trading.Indicators;

public static class IIndicatorExtensions
{
    /// <summary>
    /// Adds a new source value to the indicator.
    /// </summary>
    public static void AddRange<TSource>(this IIndicatorSource<TSource> indicator, IEnumerable<TSource> values)
    {
        Guard.IsNotNull(indicator, nameof(indicator));
        Guard.IsNotNull(values, nameof(values));

        foreach (var value in values)
        {
            indicator.Add(value);
        }
    }
}