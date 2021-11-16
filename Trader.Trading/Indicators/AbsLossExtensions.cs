using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class AbsLossExtensions
{
    /// <summary>
    /// Calculates the absolute loss between the current value and the previous value over the specified source.
    /// Evaluates to zero if there is no loss.
    /// </summary>
    /// <param name="source">The source for absolute loss calculation.</param>
    public static IEnumerable<decimal> AbsLoss(this IEnumerable<decimal> source)
    {
        return new AbsLossIterator(source);
    }

    /// <inheritdoc cref="AbsLoss(IEnumerable{decimal})"/>
    /// <param name="selector">A transform function to apply to each element.</param>
    public static IEnumerable<decimal> AbsLoss<T>(this IEnumerable<T> source, Func<T, decimal> selector)
    {
        var transformed = source.Select(selector);

        return transformed.AbsLoss();
    }
}