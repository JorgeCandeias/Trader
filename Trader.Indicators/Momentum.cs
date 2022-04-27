namespace Outcompute.Trader.Indicators;

public class Momentum : CompositeIndicator<decimal?, decimal?>
{
    public const int DefaultPeriods = 10;

    public Momentum(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            return Indicator.Change(source, periods);
        })
    {
    }
}

public static partial class Indicator
{
    public static Momentum Momentum(this IndicatorResult<decimal?> source, int periods = Indicators.Momentum.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToMomentum<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Momentum.DefaultPeriods)
        => source.Select(selector).Identity().Momentum(periods);

    public static IEnumerable<decimal?> Momentum(this IEnumerable<decimal?> source, int periods = Indicators.Momentum.DefaultPeriods)
        => source.ToMomentum(x => x, periods);
}