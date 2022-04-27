namespace Outcompute.Trader.Indicators;

public class UltimateOscillator : CompositeIndicator<HLC, decimal?>
{
    public const int DefaultFastPeriods = 7;
    public const int DefaultMediumPeriods = 14;
    public const int DefaultSlowPeriods = 28;

    public UltimateOscillator(IndicatorResult<HLC> source, int fastPeriods = DefaultFastPeriods, int mediumPeriods = DefaultMediumPeriods, int slowPeriods = DefaultSlowPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(fastPeriods, 1, nameof(fastPeriods));
            Guard.IsGreaterThanOrEqualTo(mediumPeriods, 1, nameof(mediumPeriods));
            Guard.IsGreaterThanOrEqualTo(slowPeriods, 1, nameof(slowPeriods));

            static IndicatorResult<decimal?> Average(IndicatorResult<decimal?> bp, IndicatorResult<decimal?> tr, int periods)
                => Indicator.MovingSum(bp, periods) / Indicator.MovingSum(tr, periods);

            var close = Indicator.Transform(x, x => x.Close);
            var prev = Indicator.Previous(close);

            // todo: replace with true range
            var high = Indicator.Transform(x, x => x.High);
            var low = Indicator.Transform(x, x => x.Low);
            var highx = Indicator.Max(high, prev);
            var lowx = Indicator.Min(low, prev);

            var bp = close - lowx;
            var trx = highx - lowx;
            var fastAverage = Average(bp, trx, fastPeriods);
            var mediumAverage = Average(bp, trx, mediumPeriods);
            var slowAverage = Average(bp, trx, slowPeriods);

            return 100M * ((4M * fastAverage) + (2M * mediumAverage) + slowAverage) / 7M;
        })
    {
        FastPeriods = fastPeriods;
        MediumPeriods = mediumPeriods;
        SlowPeriods = slowPeriods;
    }

    public int FastPeriods { get; }
    public int MediumPeriods { get; }
    public int SlowPeriods { get; }
}

public static partial class Indicator
{
    public static UltimateOscillator UltimateOscillator(
        this IndicatorResult<HLC> source,
        int fastPeriods = Indicators.UltimateOscillator.DefaultFastPeriods,
        int mediumPeriods = Indicators.UltimateOscillator.DefaultMediumPeriods,
        int slowPeriods = Indicators.UltimateOscillator.DefaultSlowPeriods)
        => new(source, fastPeriods, mediumPeriods, slowPeriods);

    public static IEnumerable<decimal?> ToUltimateOscillator<T>(
        this IEnumerable<T> source,
        Func<T, decimal?> highSelector,
        Func<T, decimal?> lowSelector,
        Func<T, decimal?> closeSelector,
        int fastPeriods = Indicators.UltimateOscillator.DefaultFastPeriods,
        int mediumPeriods = Indicators.UltimateOscillator.DefaultMediumPeriods,
        int slowPeriods = Indicators.UltimateOscillator.DefaultSlowPeriods)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().UltimateOscillator(fastPeriods, mediumPeriods, slowPeriods);
}