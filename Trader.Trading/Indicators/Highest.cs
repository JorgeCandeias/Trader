using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class Highest : IndicatorBase<decimal?, decimal?>
{
    public Highest(int periods = 1, bool outputWarmup = false)
    {
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        Periods = periods;
        OutputWarmup = outputWarmup;
    }

    public Highest(IIndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false) : this(periods, outputWarmup)
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
            highest = MathN.Max(Source[i], highest, MinMaxBehavior.NonNullWins);
        }

        return highest;
    }
}

public static partial class Indicator
{
    public static Highest Highest(int periods = 1, bool outputWarmup = false) => new(periods, outputWarmup);

    public static Highest Highest(IIndicatorResult<decimal?> source, int periods = 1, bool outputWarmup = false) => new(source, periods, outputWarmup);

    public static Highest Highest(IIndicatorResult<HLC> source, int periods = 1, bool outputWarmup = false) => new(Transform(source, x => x.High), periods, outputWarmup);
}

public static class HighestEnumerableExtensions
{
    public static IEnumerable<decimal?> Highest<T>(this IEnumerable<T> source, Func<T, decimal?> selector, int periods = 1, bool outputWarmup = false)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Highest(periods, outputWarmup);

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Highest(this IEnumerable<Kline> source, int periods = 1, bool outputWarmup = false)
    {
        return source.Highest(x => x.HighPrice, periods, outputWarmup);
    }

    public static IEnumerable<decimal?> Highest(this IEnumerable<decimal?> source, int periods = 1, bool outputWarmup = false)
    {
        return source.Highest(x => x, periods, outputWarmup);
    }
}