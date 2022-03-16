namespace Outcompute.Trader.Trading.Indicators;

public class WmaIndicator : IndicatorBase<decimal?, decimal?>
{
    public WmaIndicator(int periods = 10)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
    }

    public WmaIndicator(IIndicatorResult<decimal?> source, int periods = 10) : this(periods)
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
        var sum = 0M;

        var start = Math.Max(index - Periods + 1, 0);
        var end = index + 1;
        for (var i = start; i < end; i++)
        {
            var value = Source[i];
            if (value.HasValue)
            {
                var weight = ++factor * Periods;
                norm += weight;
                sum += value.Value * weight;
            }
        }

        if (norm > 0)
        {
            return sum / norm;
        }

        return null;
    }
}

public static class WeightedMovingAverageExtensions
{
    public static IEnumerable<decimal?> WeightedMovingAverage(this IEnumerable<decimal?> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var indicator = new WmaIndicator(periods);

        foreach (var item in source)
        {
            indicator.Add(item);

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> WeightedMovingAverage(this IEnumerable<Kline> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        return source.WeightedMovingAverage(x => x.ClosePrice, periods);
    }

    public static IEnumerable<decimal?> WeightedMovingAverage(this IEnumerable<decimal> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        return source.WeightedMovingAverage(x => x, periods);
    }

    public static IEnumerable<decimal?> WeightedMovingAverage<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        return source.Select(selector).WeightedMovingAverage(periods);
    }
}