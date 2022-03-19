namespace Outcompute.Trader.Trading.Indicators;

public class Ema : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public Ema(IndicatorResult<decimal?> source, int periods = DefaultPeriods) : base(source, true)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        Alpha = 2M / (periods + 1M);

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

        var ema = Result[index - 1];

        // start from the sma to avoid spikes
        if (!ema.HasValue)
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

        // calculate the next ema
        var next = Source[index];
        if (next.HasValue)
        {
            return (Alpha * next) + (1 - Alpha) * ema;
        }
        else
        {
            return next;
        }
    }
}

public static partial class Indicator
{
    public static Ema Ema(this IndicatorResult<decimal?> source, int periods = Indicators.Ema.DefaultPeriods)
        => new(source, periods);

    public static IEnumerable<decimal?> ToEma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Ema.DefaultPeriods)
        => source.Select(selector).Identity().Ema(periods);

    public static IEnumerable<decimal?> ToEma(this IEnumerable<Kline> source, int periods = Indicators.Ema.DefaultPeriods)
        => source.ToEma(x => x.ClosePrice, periods);

    public static IEnumerable<decimal?> ToEma(this IEnumerable<decimal?> source, int periods = Indicators.Ema.DefaultPeriods)
        => source.ToEma(x => x, periods);
}