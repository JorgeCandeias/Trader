using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class Lowest : IndicatorBase<decimal?, decimal?>
{
    public Lowest(int periods = 1)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
    }

    public Lowest(IIndicatorResult<decimal?> source, int periods = 1) : this(periods)
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
            highest = MathN.Min(Source[i], highest, MinMaxBehavior.NonNullWins);
        }

        return highest;
    }
}

public static partial class Indicator
{
    public static Lowest Lowest(int periods = 1) => new(periods);

    public static Lowest Lowest(IIndicatorResult<decimal?> source, int periods = 1) => new(source, periods);
}

public static class LowestEnumerableExtensions
{
    /// <summary>
    /// Yields the lowest value in <paramref name="source"/> within <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<decimal?> Lowest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Lowest(periods);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Lowest(this IEnumerable<Kline> source, int periods = 1)
    {
        return source.Lowest(x => x.LowPrice, periods);
    }
}