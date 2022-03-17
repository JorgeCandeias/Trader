namespace Outcompute.Trader.Trading.Indicators;

public static class CommodityChannelIndexExtensions
{
    public static IEnumerable<decimal?> CommodityChannelIndex<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 20)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var selected = source.Select(selector);
        var ma = selected.Sma(periods);
        var dev = selected.SmaDev(periods);

        return selected.Zip(ma, dev).Select(x => (x.First - x.Second) / (0.015M * x.Third));
    }

    public static IEnumerable<decimal?> CommodityChannelIndex(this IEnumerable<Kline> source, int periods = 20)
    {
        return source.HLC3().CommodityChannelIndex(x => x, periods);
    }
}

/*
public class CommodityChannelIndex : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 20;

    private readonly Identity<decimal?> _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public CommodityChannelIndex(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        _source = Indicator.Identity<decimal?>();

        var ma = Indicator.Sma(_source, periods);
        var dev = Indicator

        _indicator = 

        Periods = periods;
    }

    public CommodityChannelIndex(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
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
*/