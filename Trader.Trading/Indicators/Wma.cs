namespace Outcompute.Trader.Trading.Indicators;

public class Wma : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public Wma(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
    }

    public Wma(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
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
    public static Wma Wma(int periods = Indicators.Wma.DefaultPeriods) => new(periods);

    public static Wma Wma(IIndicatorResult<decimal?> source, int periods = Indicators.Wma.DefaultPeriods) => new(source, periods);
}

public static class WeightedMovingAverageExtensions
{
    public static IEnumerable<decimal?> Wma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Wma.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        var indicator = Indicator.Wma(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Wma(this IEnumerable<Kline> source, int periods = Indicators.Wma.DefaultPeriods)
    {
        return source.Wma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> Wma(this IEnumerable<decimal?> source, int periods = Indicators.Wma.DefaultPeriods)
    {
        return source.Wma(x => x, periods);
    }
}