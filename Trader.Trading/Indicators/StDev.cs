using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class StDev : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    private readonly Identity<decimal?> _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public StDev(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        _source = Indicator.Identity<decimal?>();
        _indicator = Indicator.Transform(Indicator.Variance(_source, periods), x => MathN.Sqrt(x));

        Periods = periods;
    }

    public StDev(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
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
    public static StDev StDev(int periods = Indicators.StDev.DefaultPeriods) => new(periods);

    public static StDev StDev(IIndicatorResult<decimal?> source, int periods = Indicators.StDev.DefaultPeriods) => new(source, periods);
}

public static class StdDevEnumerableExtensions
{
    public static IEnumerable<decimal?> StDev<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.StDev.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.StDev(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> StDev(this IEnumerable<decimal?> source, int periods = Indicators.StDev.DefaultPeriods)
    {
        return source.StDev(x => x, periods);
    }
}