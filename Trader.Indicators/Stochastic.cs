namespace Outcompute.Trader.Indicators;

public class Stochastic : CompositeIndicator<HLC, decimal?>
{
    public const int DefaultPeriods = 20;

    public Stochastic(IndicatorResult<HLC> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            var high = source.Transform(x => x.High);
            var low = source.Transform(x => x.Low);
            var close = source.Transform(x => x.Close);
            var highest = high.Highest(periods);
            var lowest = low.Lowest(periods);

            return 100M * (close - lowest) / (highest - lowest);
        })
    {
        Periods = periods;
    }

    public int Periods { get; }
}

public static partial class Indicator
{
    public static Stochastic Stochastic(this IndicatorResult<HLC> source, int periods = Indicators.Stochastic.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToStochastic<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = Indicators.Stochastic.DefaultPeriods)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().Stochastic(periods);
}