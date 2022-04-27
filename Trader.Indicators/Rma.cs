namespace Outcompute.Trader.Indicators;

public class Rma : IndicatorBase<decimal?, decimal?>
{
    public const int DefaultPeriods = 10;

    public Rma(IndicatorResult<decimal?> source, int periods = DefaultPeriods)
        : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        Alpha = 1M / Periods;

        Ready();
    }

    public int Periods { get; }

    public decimal Alpha { get; }

    protected override decimal? Calculate(int index)
    {
        // skip until the indicator is seeded
        if (index < Periods - 1)
        {
            return null;
        }

        var rma = index < 1 ? null : Result[index - 1];

        // start from the sma to avoid spikes
        if (!rma.HasValue)
        {
            decimal? sum = 0M;
            var count = 0;

            for (var i = Math.Max(0, index - Periods + 1); i <= index; i++)
            {
                sum += Source[i];
                count++;
            }

            return count > 0 ? sum / count : null;
        }

        // calculate the next rma
        var next = Source[index];
        if (next.HasValue)
        {
            return (Alpha * next) + (1 - Alpha) * rma;
        }
        else
        {
            return next;
        }
    }
}

public static partial class Indicator
{
    public static Rma Rma(this IndicatorResult<decimal?> source, int periods = Indicators.Rma.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToRma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Rma.DefaultPeriods)
        => source.Select(selector).Identity().Rma(periods);

    public static IEnumerable<decimal?> ToRma(this IEnumerable<decimal?> source, int periods = Indicators.Rma.DefaultPeriods)
        => source.ToRma(x => x, periods);
}