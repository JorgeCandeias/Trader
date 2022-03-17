namespace Outcompute.Trader.Trading.Indicators;

public class Hma : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    private readonly IIndicator<decimal?, decimal?> _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public Hma(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        _source = Indicator.Identity<decimal?>();
        _indicator = Indicator.Wma(2M * Indicator.Wma(_source, periods / 2) - Indicator.Wma(_source, periods), (int)Math.Floor(Math.Sqrt(periods)));

        Periods = periods;
    }

    public Hma(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
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
    public static Hma Hma(int periods = Indicators.Hma.DefaultPeriods) => new(periods);

    public static Hma Hma(IIndicatorResult<decimal?> source, int periods = Indicators.Hma.DefaultPeriods) => new(source, periods);
}

public static class HmaEnumerableExtensions
{
    public static IEnumerable<decimal?> Hma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Hma.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Hma(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Hma(this IEnumerable<Kline> source, int periods = Indicators.Hma.DefaultPeriods)
    {
        return source.Hma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> Hma(this IEnumerable<decimal?> source, int periods = Indicators.Hma.DefaultPeriods)
    {
        return source.Hma(x => x, periods);
    }
}