namespace Outcompute.Trader.Trading.Indicators;

public class Stochastic : IndicatorBase<HLC, decimal?>
{
    internal const int DefaultPeriods = 20;

    private readonly Identity<HLC> _source;
    private readonly IIndicatorResult<decimal?> _stoch;

    public Stochastic(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;

        _source = new Identity<HLC>();

        var high = Indicator.Transform(_source, x => x.High);
        var low = Indicator.Transform(_source, x => x.Low);
        var close = Indicator.Transform(_source, x => x.Close);
        var highest = Indicator.Highest(high, periods);
        var lowest = Indicator.Lowest(low, periods);

        _stoch = 100M * (close - lowest) / (highest - lowest);
    }

    public Stochastic(IIndicatorResult<HLC> source, int periods = DefaultPeriods) : this(periods)
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
        return _stoch[index];
    }
}

public static partial class Indicator
{
    public static Stochastic Stochastic(int periods = Indicators.Stochastic.DefaultPeriods) => new(periods);

    public static Stochastic Stochastic(IIndicatorResult<HLC> source, int periods = Indicators.Stochastic.DefaultPeriods) => new(source, periods);
}

public static class StochasticEnumerableExtensions
{
    public static IEnumerable<decimal?> Stochastic<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = Indicators.Stochastic.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));

        using var indicator = Indicator.Stochastic(periods);

        foreach (var item in source)
        {
            indicator.Add(new HLC(highSelector(item), lowSelector(item), closeSelector(item)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Stochastic(this IEnumerable<Kline> source, int periods = Indicators.Stochastic.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));

        return source.Stochastic(x => x.ClosePrice, x => x.HighPrice, x => x.LowPrice, periods);
    }
}