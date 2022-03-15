namespace Outcompute.Trader.Trading.Indicators;

public class RmaIndicator : IndicatorBase<decimal?, decimal?>
{
    public RmaIndicator(int periods = 10)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        Alpha = 1M / Periods;
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

        var rma = Result[^2];

        // start from the sma to avoid spikes
        if (!rma.HasValue)
        {
            var sum = 0M;
            var count = 0;

            for (var i = 0; i < Periods; i++)
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

        // calculate the next rma
        var next = Source[^1];
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

public static class RmaIndicatorEnumerableExtensions
{
    public static IEnumerable<decimal?> RunningMovingAverage(this IEnumerable<decimal?> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var indicator = new RmaIndicator(periods);

        foreach (var item in source)
        {
            indicator.Add(item);

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> RunningMovingAverage(this IEnumerable<Kline> source, int periods)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .RunningMovingAverage(periods);
    }
}