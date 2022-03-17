using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Calculates the loss between the current value and the previous value.
/// </summary>
public class Loss : IndicatorBase<decimal?, decimal?>
{
    public Loss()
    {
    }

    public Loss(IIndicatorResult<decimal?> source) : this()
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

        return MathN.Min(Source[index] - Source[index - 1], 0, MinMaxBehavior.NullWins);
    }
}

public static partial class Indicator
{
    public static Loss Loss() => new();

    public static Loss Loss(IIndicatorResult<decimal?> source) => new(source);
}

public static class LossEnumerableExtensions
{
    public static IEnumerable<decimal?> Loss(this IEnumerable<decimal?> source)
    {
        return source.Loss(x => x);
    }

    public static IEnumerable<decimal?> Loss<T>(this IEnumerable<T> source, Func<T, decimal?> selector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(selector, nameof(selector));

        using var indicator = Indicator.Loss();

        foreach (var item in source)
        {
            indicator.Add(selector(item));

            yield return indicator[^1];
        }
    }
}