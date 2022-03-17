namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Calculates the Commodity Channel Index.
/// </summary>
public class Cci : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 20;

    private readonly Identity<decimal?> _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public Cci(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        _source = Indicator.Identity<decimal?>();
        _indicator = (_source - Indicator.Sma(_source, periods)) / (0.015M * Indicator.SmaDev(_source, periods));

        Periods = periods;
    }

    public Cci(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int Periods { get; }

    protected override decimal? Calculate(int index)
    {
        // update the core source and cascade
        _source.Update(index, Source[index]);

        // return the final result
        return _indicator[index];
    }
}

public static partial class Indicator
{
    public static Cci Cci(int periods = Indicators.Cci.DefaultPeriods) => new(periods);

    public static Cci Cci(IIndicatorResult<decimal?> source, int periods = Indicators.Cci.DefaultPeriods) => new(source, periods);
}

public static class CciEnumerableExtensions
{
    public static IEnumerable<decimal?> Cci<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Cci.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Cci(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Cci(this IEnumerable<Kline> source, int periods = Indicators.Cci.DefaultPeriods)
    {
        return source.HLC3().Cci(x => x, periods);
    }
}