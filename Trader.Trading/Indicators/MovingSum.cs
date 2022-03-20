namespace Outcompute.Trader.Trading.Indicators;

public class MovingSum : CompositeIndicator<decimal?, decimal?>
{
    internal const int DefaultPeriods = 1;

    public MovingSum(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, source =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            return Indicator.MovingWindow(source, periods).Transform(x => x.Sum());
        })
    {
    }
}

public static partial class Indicator
{
    public static MovingSum MovingSum(this IndicatorResult<decimal?> source, int periods = Indicators.MovingSum.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToMovingSum<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.MovingSum.DefaultPeriods)
        => source.Select(selector).Identity().MovingSum(periods);

    public static IEnumerable<decimal?> ToMovingSum(this IEnumerable<decimal?> source, int periods = Indicators.MovingSum.DefaultPeriods)
        => source.ToMovingSum(x => x, periods);
}