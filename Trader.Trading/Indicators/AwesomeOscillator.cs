namespace Outcompute.Trader.Trading.Indicators;

public class AwesomeOscillator : IndicatorBase<HL, decimal?>
{
    internal const int DefaultFastPeriods = 5;
    internal const int DefaultSlowPeriods = 34;

    private readonly HL2 _source;
    private readonly IIndicatorResult<decimal?> _indicator;

    public AwesomeOscillator(int fastPeriods = DefaultFastPeriods, int slowPeriods = DefaultSlowPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(fastPeriods, 1, nameof(fastPeriods));
        Guard.IsGreaterThanOrEqualTo(slowPeriods, 1, nameof(slowPeriods));

        FastPeriods = fastPeriods;
        SlowPeriods = slowPeriods;

        _source = Indicator.HL2();
        _indicator = Indicator.Sma(_source, fastPeriods) - Indicator.Sma(_source, slowPeriods);
    }

    public AwesomeOscillator(IIndicatorResult<HL> source, int fastPeriods = DefaultFastPeriods, int slowPeriods = DefaultSlowPeriods) : this(fastPeriods, slowPeriods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int FastPeriods { get; }

    public int SlowPeriods { get; }

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
    public static AwesomeOscillator AwesomeOscillator(int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods) => new(fastPeriods, slowPeriods);

    public static AwesomeOscillator AwesomeOscillator(IIndicatorResult<HL> source, int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods) => new(source, fastPeriods, slowPeriods);
}

public static class AwesomeOscillatorEnumerableExtensions
{
    public static IEnumerable<decimal?> AwesomeOscillator<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));

        using var indicator = Indicator.AwesomeOscillator(fastPeriods, slowPeriods);

        foreach (var item in source)
        {
            indicator.Add(new HL(highSelector(item), lowSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> AwesomeOscillator(this IEnumerable<Kline> source, int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods)
    {
        return source.AwesomeOscillator(x => x.HighPrice, x => x.LowPrice, fastPeriods, slowPeriods);
    }
}