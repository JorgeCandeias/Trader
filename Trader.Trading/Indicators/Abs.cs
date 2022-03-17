using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

/// <summary>
/// Indicator that yields the absolute value of each source value.
/// </summary>
public class Abs : Transform<decimal?, decimal?>
{
    /// <summary>
    /// Creates a new absolute indicator.
    /// </summary>
    public Abs() : base(Transform)
    {
    }

    /// <summary>
    /// Creates a new absolute indicator from the specified source indicator.
    /// </summary>
    public Abs(IIndicatorResult<decimal?> source) : base(source, Transform)
    {
    }

    private static readonly Func<decimal?, decimal?> Transform = x => MathN.Abs(x);
}

public static partial class Indicator
{
    public static Abs Abs() => new();

    public static Abs Abs(IIndicatorResult<decimal?> source) => new(source);
}

public static class AbsEnumerableExtensions
{
    public static IEnumerable<decimal?> Abs(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        using var indicator = Indicator.Abs();

        foreach (var value in source)
        {
            indicator.Add(value);

            yield return indicator[^1];
        }
    }
}