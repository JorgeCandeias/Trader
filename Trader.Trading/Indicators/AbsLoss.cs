using Outcompute.Trader.Core.Mathematics;
using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public class AbsLoss : IndicatorBase<decimal?, decimal?>
{
    /// <summary>
    /// Creates a new absolute loss indicator.
    /// </summary>
    public AbsLoss()
    {
    }

    /// <summary>
    /// Creates a new absolute loss indicator from the specified source indicator.
    /// </summary>
    public AbsLoss(IIndicatorResult<decimal?> source)
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

        return MathN.Abs(MathN.Min(Source[^1] - Source[^2], 0));
    }
}

public static class AbsLossEnumerableExtensions
{
    /// <summary>
    /// Calculates the absolute loss between the current value and the previous value over the specified source.
    /// </summary>
    /// <param name="source">The source for absolute loss calculation.</param>
    public static IEnumerable<decimal?> AbsLoss(this IEnumerable<decimal?> source)
    {
        Guard.IsNotNull(source, nameof(source));

        var indicator = new AbsLoss();

        foreach (var item in source)
        {
            indicator.Add(item);

            yield return indicator[^1];
        }
    }
}