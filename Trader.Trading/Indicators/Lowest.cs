using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class Lowest : IndicatorBase<decimal?, decimal?>
{
    public Lowest(int periods = 1, bool outputWarmup = false)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        OutputWarmup = outputWarmup;
    }

    public Lowest(IIndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false) : this(periods, outputWarmup)
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    public int Periods { get; }
    public bool OutputWarmup { get; }

    protected override decimal? Calculate(int index)
    {
        if (index < Periods - 1 && !OutputWarmup)
        {
            return null;
        }

        decimal? highest = null;
        for (var i = Math.Max(index - Periods + 1, 0); i <= index; i++)
        {
            highest = MathN.Min(Source[i], highest, MinMaxBehavior.NonNullWins);
        }

        return highest;
    }
}

public static partial class Indicator
{
    public static Lowest Lowest(int periods = 1, bool outputWarmup = false) => new(periods, outputWarmup);

    public static Lowest Lowest(IIndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false) => new(source, periods, outputWarmup);

    public static Lowest Lowest(IIndicatorResult<HL> source, int periods = 1, bool outputWarmup = false) => new(Transform(source, x => x.Low), periods, outputWarmup);

    public static Lowest Lowest(IIndicatorResult<HLC> source, int periods = 1, bool outputWarmup = false) => new(Transform(source, x => x.Low), periods, outputWarmup);
}

public static class LowestEnumerableExtensions
{
    /// <summary>
    /// Yields the lowest value in <paramref name="source"/> within <paramref name="periods"/> ago.
    /// </summary>
    public static IEnumerable<decimal?> Lowest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1, bool outputWarmup = false)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Lowest(periods, outputWarmup);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Lowest(this IEnumerable<Kline> source, int periods = 1, bool outputWarmup = false)
    {
        return source.Lowest(x => x.LowPrice, periods, outputWarmup);
    }
}