using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Calculates the gain between the current value and the previous value.
/// </summary>
public class Gain : IndicatorBase<decimal?, decimal?>
{
    public Gain()
    {
    }

    public Gain(IIndicatorResult<decimal?> source) : this()
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    protected override decimal? Calculate(int index)
    {
        if (index < 1)
        {
            return null;
        }

        return MathN.Max(Source[index] - Source[index - 1], 0, MinMaxBehavior.NullWins);
    }
}

public static partial class Indicator
{
    public static Gain Gain() => new();

    public static Gain Gain(IIndicatorResult<decimal?> source) => new(source);
}

public static class GainEnumerableExtensions
{
    public static IEnumerable<decimal?> Gain<T>(this IEnumerable<T> source, Func<T, decimal?> selector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Gain();

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> Gain(this IEnumerable<decimal?> source)
    {
        return source.Gain(x => x);
    }
}