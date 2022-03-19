namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Calculates the Commodity Channel Index.
/// </summary>
public class Cci : CompositeIndicator<decimal?, decimal?>
{
    internal const int DefaultPeriods = 20;

    public Cci(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            return (source - source.Sma(periods)) / (0.015M * source.SmaDev(periods));
        })
    {
    }
}

public static partial class Indicator
{
    public static Cci Cci(this IndicatorResult<decimal?> source, int periods = Indicators.Cci.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToCci<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Cci.DefaultPeriods)
        => source.Select(selector).Identity().Cci(periods);

    public static IEnumerable<decimal?> ToCci(this IEnumerable<Kline> source, int periods = Indicators.Cci.DefaultPeriods)
        => source.HLC3().Identity().Cci(periods);
}