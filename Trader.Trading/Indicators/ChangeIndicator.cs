namespace Outcompute.Trader.Trading.Indicators;

public class ChangeIndicator : IndicatorBase<decimal?, decimal?>
{
    protected override decimal? Calculate(int index)
    {
        if (index < 1)
        {
            return null;
        }

        return Source[^1] - Source[^2];
    }
}

public static class ChangeIndicatorEnumerableExtensions
{
    /// <summary>
    /// Yields the difference between the current value and the previous value from <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<decimal?> Change<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var indicator = new ChangeIndicator();

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    /// <inheritdoc cref="Change{T}(IEnumerable{T}, Func{T, decimal?}, int)"/>
    public static IEnumerable<decimal?> Change(this IEnumerable<decimal?> source, int periods = 1)
    {
        return source.Change(x => x, periods);
    }

    /// <inheritdoc cref="Change{T}(IEnumerable{T}, Func{T, decimal?}, int)"/>
    public static IEnumerable<decimal?> Change(this IEnumerable<decimal> source, int periods = 1)
    {
        return source.Change(x => x, periods);
    }
}