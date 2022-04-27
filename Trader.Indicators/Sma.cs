namespace Outcompute.Trader.Indicators;

public class Sma : IndicatorBase<decimal?, decimal?>
{
    public const int DefaultPeriods = 10;

    public Sma(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
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

        // calculate the next sma
        var start = Math.Max(index - Periods + 1, 0);
        var end = index + 1;
        decimal? sum = 0M;
        var count = 0;
        for (var i = start; i < end; i++)
        {
            sum += Source[i];
            count++;
        }

        return count > 0 ? sum / count : null;
    }
}

public static partial class Indicator
{
    public static Sma Sma(this IndicatorResult<decimal?> source, int periods = Indicators.Sma.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToSma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Sma.DefaultPeriods)
        => source.Select(selector).Identity().Sma(periods);

    public static IEnumerable<decimal?> ToSma(this IEnumerable<decimal?> source, int periods = Indicators.Sma.DefaultPeriods)
        => source.ToSma(x => x, periods);
}