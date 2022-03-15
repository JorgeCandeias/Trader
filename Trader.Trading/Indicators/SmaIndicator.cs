namespace Outcompute.Trader.Trading.Indicators;

public class SmaIndicator : IndicatorBase<decimal?, decimal?>
{
    public SmaIndicator(int length = 10)
    {
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        Length = length;
    }

    public int Length { get; }

    protected override decimal? Calculate(int index)
    {
        // skip until the indicator is seeded
        if (index < Length - 1)
        {
            return null;
        }

        // calculate the next sma
        var start = Math.Max(index - Length + 1, 0);
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

public static class SmaIndicatorEnumerableExtensions
{
    public static IEnumerable<decimal?> SimpleMovingAverage(this IEnumerable<decimal?> source, int length)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        var indicator = new SmaIndicator(length);

        foreach (var item in source)
        {
            indicator.Add(item);

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> SimpleMovingAverage(this IEnumerable<Kline> source, int periods)
    {
        return source
            .Select(x => (decimal?)x.ClosePrice)
            .SimpleMovingAverage(periods);
    }
}