namespace Outcompute.Trader.Indicators;

public class Rsi : CompositeIndicator<decimal?, decimal?>
{
    internal const int DefaultPeriods = 14;

    public Rsi(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

            var up = source.Gain().Rma(periods);
            var down = source.AbsLoss().Rma(periods);

            return Indicator.Zip(up, down, (u, d) =>
            {
                if (d == 0) return 100;
                if (u == 0) return 0;
                return 100 - (100 / (1 + u / d));
            });
        })
    {
        Periods = periods;
    }

    public int Periods { get; }
}

public static partial class Indicator
{
    public static Rsi Rsi(this IndicatorResult<decimal?> source, int periods = Indicators.Rsi.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToRsi<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 14)
        => source.Select(selector).Identity().Rsi(periods);

    public static IEnumerable<decimal?> ToRsi(this IEnumerable<decimal?> source, int periods = 14)
        => source.ToRsi(x => x, periods);
}