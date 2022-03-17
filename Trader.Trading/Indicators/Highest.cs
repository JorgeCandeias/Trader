using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class Highest : IndicatorBase<decimal?, decimal?>
{
    public Highest(int periods = 1)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
    }

    public Highest(IIndicatorResult<decimal?> source, int periods = 1) : this(periods)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int Periods { get; }

    protected override decimal? Calculate(int index)
    {
        if (index < Periods - 1)
        {
            return null;
        }

        decimal? highest = null;
        for (var i = index - Periods + 1; i <= index; i++)
        {
            highest = MathN.Max(Source[i], highest, MinMaxBehavior.NonNullWins);
        }

        return highest;
    }
}

public static partial class Indicator
{
    public static Highest Highest(int periods = 1) => new(periods);

    public static Highest Highest(IIndicatorResult<decimal?> source, int periods = 1) => new(source, periods);
}

public static class HighestEnumerableExtensions
{
    public static IEnumerable<decimal?> Highest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Highest(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Highest(this IEnumerable<Kline> source, int periods = 1)
    {
        return source.Highest(x => x.HighPrice, periods);
    }

    public static IEnumerable<decimal?> Highest(this IEnumerable<decimal?> source, int periods = 1)
    {
        return source.Highest(x => x, periods);
    }
}