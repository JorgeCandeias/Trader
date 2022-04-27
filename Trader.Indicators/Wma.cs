namespace Outcompute.Trader.Indicators;

public class Wma : IndicatorBase<decimal?, decimal?>
{
    public const int DefaultPeriods = 10;

    public Wma(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;

        Ready();
    }

    public int Periods { get; }

    protected override decimal? Calculate(int index)
    {
        // skip until the indicator is seeded
        if (index < Periods - 1)
        {
            return null;
        }

        // calculate the next wma
        var factor = 0;
        var norm = 0M;
        decimal? sum = 0M;

        var start = Math.Max(index - Periods + 1, 0);
        var end = index + 1;
        for (var i = start; i < end; i++)
        {
            var weight = ++factor * Periods;
            norm += weight;
            sum += Source[i] * weight;
        }

        return norm > 0 ? sum / norm : null;
    }
}

public static partial class Indicator
{
    public static Wma Wma(this IndicatorResult<decimal?> source, int periods = Indicators.Wma.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToWma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Wma.DefaultPeriods)
        => source.Select(selector).Identity().Wma(periods);

    public static IEnumerable<decimal?> ToWma(this IEnumerable<decimal?> source, int periods = Indicators.Wma.DefaultPeriods)
        => source.ToWma(x => x, periods);
}