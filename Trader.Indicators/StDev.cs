using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

public class StDev : CompositeIndicator<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public StDev(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            return source.Variance(periods).Transform(x => MathN.Sqrt(x));
        })
    {
        Periods = periods;
    }

    public int Periods { get; }
}

public static partial class Indicator
{
    public static StDev StDev(this IndicatorResult<decimal?> source, int periods = Indicators.StDev.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToStDev<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.StDev.DefaultPeriods)
        => source.Select(selector).Identity().StDev(periods);

    public static IEnumerable<decimal?> ToStDev(this IEnumerable<decimal?> source, int periods = Indicators.StDev.DefaultPeriods)
        => source.ToStDev(x => x, periods);
}