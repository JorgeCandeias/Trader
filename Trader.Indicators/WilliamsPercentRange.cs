namespace Outcompute.Trader.Indicators;

public class WilliamsPercentRange : CompositeIndicator<HLC, decimal?>
{
    public const int DefaultPeriods = 14;

    public WilliamsPercentRange(IndicatorResult<HLC> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            var max = Indicator.Highest(x, periods);
            var min = Indicator.Lowest(x, periods);
            var close = Indicator.Transform(x, x => x.Close);

            return 100M * (close - max) / (max - min);
        })
    {
        Periods = periods;
    }

    public int Periods { get; }
}

public static partial class Indicator
{
    public static WilliamsPercentRange WilliamsPercentRange(this IndicatorResult<HLC> source, int periods = Indicators.WilliamsPercentRange.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToWilliamsPercentRange<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = Indicators.WilliamsPercentRange.DefaultPeriods)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().WilliamsPercentRange(periods);
}