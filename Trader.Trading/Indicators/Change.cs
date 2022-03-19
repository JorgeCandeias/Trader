namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Indicator that yields the change between the current value and the previous value.
/// </summary>
public class Change : IndicatorBase<decimal?, decimal?>
{
    private readonly int _periods;

    /// <summary>
    /// Creates a new change indicator from the specified source indicator.
    /// </summary>
    public Change(IndicatorResult<decimal?> source, int periods = 1) : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        _periods = periods;

        Ready();
    }

    protected override decimal? Calculate(int index)
    {
        if (index < _periods)
        {
            return null;
        }

        return Source[index] - Source[index - _periods];
    }
}

public static partial class Indicator
{
    public static Change Change(this IndicatorResult<decimal?> source, int periods = 1)
        => new(source, periods);

    public static IEnumerable<decimal?> ToChange<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
        => source.Select(selector).Identity().Change(periods);

    public static IEnumerable<decimal?> ToChange(this IEnumerable<decimal?> source, int periods = 1)
        => source.ToChange(x => x, periods);

    public static IEnumerable<decimal?> ToChange(this IEnumerable<decimal> source, int periods = 1)
        => source.ToChange(x => x, periods);
}