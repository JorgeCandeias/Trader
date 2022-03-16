namespace Outcompute.Trader.Trading.Indicators;

public class Rma : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public Rma(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        Alpha = 1M / Periods;
    }

    public Rma(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
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

        var rma = Result[index - 1];

        // start from the sma to avoid spikes
        if (!rma.HasValue)
        {
            var sum = 0M;
            var count = 0;

            for (var i = 0; i <= index; i++)
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

            return null;
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

public static class RmaEnumerableExtensions
{
    public static IEnumerable<decimal?> Rma<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Rma.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = new Rma(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Rma(this IEnumerable<Kline> source, int periods = Indicators.Rma.DefaultPeriods)
    {
        return source.Rma(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> Rma(this IEnumerable<decimal?> source, int periods = Indicators.Rma.DefaultPeriods)
    {
        return source.Rma(x => x, periods);
    }
}