using static Outcompute.Trader.Trading.Indicators.Indicator;

namespace Outcompute.Trader.Trading.Indicators;

public class Hma : CompositeIndicator<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public Hma(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

            return Indicator.Wma(2M * Indicator.Wma(source, periods / 2) - Indicator.Wma(source, periods), (int)Math.Floor(Math.Sqrt(periods)));
        })
    {
        Periods = periods;
    }

    public int Periods { get; }
}

public static partial class Indicator
{
    public static Hma Hma(this IndicatorResult<decimal?> source, int periods = Indicators.Hma.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToHma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Hma.DefaultPeriods)
        => source.Select(selector).Identity().Hma(periods);

    public static IEnumerable<decimal?> ToHma(this IEnumerable<Kline> source, int periods = Indicators.Hma.DefaultPeriods)
        => source.ToHma(x => x.ClosePrice, periods);

    public static IEnumerable<decimal?> ToHma(this IEnumerable<decimal?> source, int periods = Indicators.Hma.DefaultPeriods)
        => source.ToHma(x => x, periods);
}