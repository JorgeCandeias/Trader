namespace Outcompute.Trader.Trading.Indicators;

public class Ema : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public Ema(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        Alpha = 2M / (periods + 1M);
    }

    public Ema(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
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
    public static Ema Ema(int periods = Indicators.Ema.DefaultPeriods) => new(periods);

    public static Ema Ema(IIndicatorResult<decimal?> source, int periods = Indicators.Ema.DefaultPeriods) => new(source, periods);
}

public static class EmaIndicatorEnumerableExtensions
{
    public static IEnumerable<decimal?> Ema<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Ema.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        var indicator = Indicator.Ema(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Ema(this IEnumerable<Kline> source, int periods = Indicators.Ema.DefaultPeriods)
    {
        return source.Ema(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> Ema(this IEnumerable<decimal?> source, int periods = Indicators.Ema.DefaultPeriods)
    {
        return source.Ema(x => x, periods);
    }
}