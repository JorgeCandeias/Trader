namespace Outcompute.Trader.Trading.Indicators;

public class Vwma : IndicatorBase<CV, decimal?>
{
    internal const int DefaultPeriods = 20;

    private readonly Identity<CV> _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public Vwma(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;

        _source = new Identity<CV>();
        _indicator = Indicator.Sma(Indicator.Transform(_source, x => x.Close * x.Volume), periods) / Indicator.Sma(Indicator.Transform(_source, x => x.Volume), periods);
    }

    public Vwma(IIndicatorResult<CV> source, int periods = DefaultPeriods) : this(periods)
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
    public static Vwma Vwma(int periods = Indicators.Vwma.DefaultPeriods) => new(periods);

    public static Vwma Vwma(IIndicatorResult<CV> source, int periods = Indicators.Vwma.DefaultPeriods) => new(source, periods);
}

public static class VwmaEnumerableExtensions
{
    public static IEnumerable<decimal?> Vwma<T>(this IEnumerable<T> source, Func<T, decimal?> closeSelector, Func<T, decimal?> volumeSelector, int periods = Indicators.Vwma.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));
        Guard.IsNotNull(volumeSelector, nameof(volumeSelector));

        using var indicator = Indicator.Vwma(periods);

        foreach (var item in source)
        {
            indicator.Add(new CV(closeSelector(item), volumeSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> VolumeWeightedMovingAverage(this IEnumerable<Kline> source, int periods = Indicators.Vwma.DefaultPeriods)
    {
        return source.Vwma(x => x.ClosePrice, x => x.Volume, periods);
    }
}