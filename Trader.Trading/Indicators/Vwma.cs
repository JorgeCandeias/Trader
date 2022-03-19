namespace Outcompute.Trader.Trading.Indicators;

public class Vwma : CompositeIndicator<CV, decimal?>
{
    internal const int DefaultPeriods = 20;

    public Vwma(IndicatorResult<CV> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            var cv = Indicator.Transform(source, x => x.Close * x.Volume);
            var v = Indicator.Transform(source, x => x.Volume);

            return Indicator.Sma(cv, periods) / Indicator.Sma(v, periods);
        })
    {
        Periods = periods;
    }

    public int Periods { get; }
}

public static partial class Indicator
{
    public static Vwma Vwma(this IndicatorResult<CV> source, int periods = Indicators.Vwma.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToVwma<T>(this IEnumerable<T> source, Func<T, decimal?> closeSelector, Func<T, decimal?> volumeSelector, int periods = Indicators.Vwma.DefaultPeriods)
        => source.Select(x => new CV(closeSelector(x), volumeSelector(x))).Identity().Vwma(periods);

    public static IEnumerable<decimal?> ToVwma(this IEnumerable<Kline> source, int periods = Indicators.Vwma.DefaultPeriods)
        => source.ToVwma(x => x.ClosePrice, x => x.Volume, periods);
}