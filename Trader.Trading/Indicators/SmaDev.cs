using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Calculates the difference between the source series and its SMA.
/// </summary>
public class SmaDev : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    private readonly IIndicator<decimal?, decimal?> _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public SmaDev(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        _source = Indicator.Identity<decimal?>();

        _indicator = Indicator.Zip(
            Indicator.Sma(_source, periods),
            Indicator.MovingWindow(_source, periods),
            (m, w) => w is null || m is null ? null : w.Sum(x => MathN.Abs(x - m)) / periods);

        Periods = periods;
    }

    public SmaDev(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
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
    public static SmaDev SmaDev(int periods = Indicators.SmaDev.DefaultPeriods) => new(periods);

    public static SmaDev SmaDev(IIndicatorResult<decimal?> source, int periods = Indicators.SmaDev.DefaultPeriods) => new(source, periods);
}

public static class SmaDevEnumerableExtensions
{
    public static IEnumerable<decimal?> SmaDev<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.SmaDev.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.SmaDev(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> SmaDev(this IEnumerable<Kline> source, int periods = Indicators.SmaDev.DefaultPeriods)
    {
        return source.SmaDev(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> SmaDev(this IEnumerable<decimal?> source, int periods = Indicators.SmaDev.DefaultPeriods)
    {
        return source.SmaDev(x => x, periods);
    }
}