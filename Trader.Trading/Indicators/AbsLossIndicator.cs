using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public class AbsLossIndicator : IndicatorBase<decimal?, decimal?>
{
    protected override decimal? Calculate(int index)
    {
        if (index < 1)
        {
            return null;
        }

        return MathN.Abs(MathN.Min(Source[^1] - Source[^2], 0));
    }
}

public static class AbsLossExtensions
{
    /// <summary>
    /// Calculates the absolute loss between the current value and the previous value over the specified source.
    /// </summary>
    /// <param name="source">The source for absolute loss calculation.</param>
    public static IEnumerable<decimal?> AbsLoss(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        var indicator = new AbsLossIndicator();

        foreach (var item in source)
        {
            indicator.Add(item);

            yield return indicator[^1];
        }
    }
}