using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class RmaExtensions
{
    /// <summary>
    /// Calculates the Running Moving Average over the specified source.
    /// </summary>
    /// <param name="source">The source for RMA calculation.</param>
    /// <param name="periods">The number of periods for RMA calculation.</param>
    public static IEnumerable<decimal> Rma(this IEnumerable<decimal> source, int periods)
    {
        return new RmaIterator(source, periods);
    }

    /// <inheritdoc cref="Rma(IEnumerable{decimal}, int)"/>
    /// <param name="selector">A transform function to apply to each element.</param>
    public static IEnumerable<decimal> Rma<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods)
    {
        var transformed = source.Select(selector);

        return transformed.Rma(periods);
    }
}