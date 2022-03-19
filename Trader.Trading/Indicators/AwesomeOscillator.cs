namespace Outcompute.Trader.Trading.Indicators;

public class AwesomeOscillator : CompositeIndicator<HL, decimal?>
{
    internal const int DefaultFastPeriods = 5;
    internal const int DefaultSlowPeriods = 34;

    public AwesomeOscillator(IndicatorResult<HL> source, int fastPeriods = DefaultFastPeriods, int slowPeriods = DefaultSlowPeriods)
        : base(source, x =>
        {
            Guard.IsGreaterThanOrEqualTo(fastPeriods, 1, nameof(fastPeriods));
            Guard.IsGreaterThanOrEqualTo(slowPeriods, 1, nameof(slowPeriods));

            var hl2 = Indicator.HL2(source);

            return Indicator.Sma(hl2, fastPeriods) - Indicator.Sma(hl2, slowPeriods);
        })
    {
    }
}

public static partial class Indicator
{
    public static AwesomeOscillator AwesomeOscillator(this IndicatorResult<HL> source, int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods)
        => new(source, fastPeriods, slowPeriods);

    public static IEnumerable<decimal?> ToAwesomeOscillator<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods)
        => source.Select(x => new HL(highSelector(x), lowSelector(x))).Identity().AwesomeOscillator(fastPeriods, slowPeriods);

    public static IEnumerable<decimal?> ToAwesomeOscillator(this IEnumerable<Kline> source, int fastPeriods = Indicators.AwesomeOscillator.DefaultFastPeriods, int slowPeriods = Indicators.AwesomeOscillator.DefaultSlowPeriods)
        => source.ToAwesomeOscillator(x => x.HighPrice, x => x.LowPrice, fastPeriods, slowPeriods);
}