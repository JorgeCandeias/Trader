namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Indicator that yields the change between the current value and the previous value.
/// </summary>
public class Change : IndicatorBase<decimal?, decimal?>
{
    /// <summary>
    /// Creates a new change indicator.
    /// </summary>
    public Change(int periods = 1)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
    }

    /// <summary>
    /// Creates a new change indicator from the specified source indicator.
    /// </summary>
    public Change(IIndicatorResult<decimal?> source, int periods = 1) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int Periods { get; }

    protected override decimal? Calculate(int index)
    {
        if (index < Periods)
        {
            return null;
        }

        return Source[index] - Source[index - Periods];
    }
}

public static partial class Indicator
{
    public static Change Change(int periods = 1) => new(periods);

    public static Change Change(IIndicatorResult<decimal?> source, int periods = 1) => new(source, periods);
}

public static class ChangeEnumerableExtensions
{
    /// <summary>
    /// Yields the difference between the current value and the previous value from <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<decimal?> Change<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        var indicator = Indicator.Change(periods);

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