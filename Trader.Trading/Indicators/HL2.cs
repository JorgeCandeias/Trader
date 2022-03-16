using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Indicator that yields the absolute value of each source value.
/// </summary>
public class HL2 : Transform<HL, decimal?>
{
    /// <summary>
    /// Creates a new absolute indicator.
    /// </summary>
    public HL2() : base(Transform)
    {
    }

    /// <summary>
    /// Creates a new absolute indicator from the specified source indicator.
    /// </summary>
    public HL2(IIndicatorResult<HL> source) : base(source, Transform)
    {
    }

    private static readonly Func<HL, decimal?> Transform = x => MathN.SafeDiv(x.High + x.Low, 2);
}

public static class HL2EnumerableExtensions
{
    public static IEnumerable<decimal?> HL2<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));

        using var indicator = new HL2();

        foreach (var value in source)
        {
            indicator.Add(new HL(highSelector(value), lowSelector(value)));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> HL2(this IEnumerable<Kline> source)
    {
        return source.HL2(x => x.HighPrice, x => x.LowPrice);
    }
}