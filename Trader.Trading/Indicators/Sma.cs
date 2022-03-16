namespace Outcompute.Trader.Trading.Indicators;

public class Sma : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public Sma(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
    }

    public Sma(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
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

        // calculate the next sma
        var start = Math.Max(index - Periods + 1, 0);
        var end = index + 1;
        var sum = 0M;
        var count = 0;
        for (var i = start; i < end; i++)
        {
            var value = Source[i];
            if (value.HasValue)
            {
                sum += value.Value;
                count++;
            }
        }
        if (count > 0)
        {
            return sum / count;
        }
        else
        {
            return null;
        }
    }
}

public static class SmaEnumerableExtensions
{
    public static IEnumerable<decimal?> Sma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Sma.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = new Sma(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Sma(this IEnumerable<Kline> source, int periods = Indicators.Sma.DefaultPeriods)
    {
        return source.Sma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> Sma(this IEnumerable<decimal?> source, int periods = Indicators.Sma.DefaultPeriods)
    {
        return source.Sma(x => x, periods);
    }
}