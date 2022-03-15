using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class AbsIndicator : TransformIndicator<decimal?, decimal?>
{
    public AbsIndicator() : base(source => MathN.Abs(source[^1]))
    {
    }
}

public static class AbsIndicatorEnumerableExtensions
{
    public static IEnumerable<decimal?> Abs(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        var indicator = new AbsIndicator();

        foreach (var value in source)
        {
            indicator.Add(value);

            yield return indicator[^1];
        }
    }
}