namespace Outcompute.Trader.Trading.Indicators;

public class Variance : IndicatorBase<decimal?, decimal?>
{
    internal const int DefaultPeriods = 10;

    public Variance(int periods = DefaultPeriods)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        Periods = periods;
    }

    public Variance(IIndicatorResult<decimal?> source, int periods = DefaultPeriods) : this(periods)
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

        // calculate the next variance
        var start = Math.Max(index - Periods + 1, 0);
        var end = index + 1;
        var sum = 0M;
        var squares = 0M;
        var count = 0;

        for (var i = start; i < end; i++)
        {
            var value = Source[i];
            if (value.HasValue)
            {
                sum += value.Value;
                squares += value.Value * value.Value;
                count++;
            }
        }

        if (count > 0)
        {
            var mean1 = sum / count;
            var mean2 = squares / count;

            return Math.Max(0, mean2 - mean1 * mean1);
        }

        return null;
    }
}

public static partial class Indicator
{
    public static Variance Variance(int periods = Indicators.Variance.DefaultPeriods) => new(periods);

    public static Variance Variance(IIndicatorResult<decimal?> source, int periods = Indicators.Variance.DefaultPeriods) => new(source, periods);
}

internal static class VarianceEnumerableExtensions
{
    public static IEnumerable<decimal?> Variance<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = Indicators.Variance.DefaultPeriods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Variance(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Variance(this IEnumerable<decimal?> source, int periods = Indicators.Variance.DefaultPeriods)
    {
        return source.Variance(x => x, periods);
    }
}