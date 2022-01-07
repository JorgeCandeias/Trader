namespace System.Collections.Generic;

public static class AverageTrueRangeRatesExtensions
{
    /// <summary>
    /// Yields average true range divided by the close price.
    /// </summary>
    public static IEnumerable<decimal> AverageTrueRangeRates(this IEnumerable<Kline> source, int periods)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        var atr = source.AverageTrueRanges(periods).GetEnumerator();
        var values = source.GetEnumerator();

        while (values.MoveNext() && atr.MoveNext())
        {
            yield return atr.Current / values.Current.ClosePrice;
        }
    }
}