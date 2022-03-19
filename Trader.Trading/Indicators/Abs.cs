using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Indicator that yields the absolute value of each source value.
/// </summary>
public class Abs : Transform<decimal?, decimal?>
{
    /// <summary>
    /// Creates a new absolute indicator from the specified source indicator.
    /// </summary>
    public Abs(IndicatorResult<decimal?> source)
        : base(source, x => MathN.Abs(x))
    {
    }
}

public static partial class Indicator
{
    public static Abs Abs(this IndicatorResult<decimal?> source)
        => new(source);

    public static IEnumerable<decimal?> ToAbs(this IEnumerable<decimal?> source)
        => source.Identity().Abs();
}