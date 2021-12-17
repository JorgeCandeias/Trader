namespace System.Collections.Generic;

internal static class TrueRangeExtensions
{
    public static IEnumerable<decimal> TrueRange(this IEnumerable<Kline> source)
    {
        Guard.IsNotNull(source, nameof(source));

        var enumerator = source.GetEnumerator();

        Kline prev;

        // calculate the first period
        if (enumerator.MoveNext())
        {
            var kline = enumerator.Current;

            yield return kline.HighPrice - kline.LowPrice;

            prev = kline;

            // calculate the remaining periods
            while (enumerator.MoveNext())
            {
                kline = enumerator.Current;

                var highLow = kline.HighPrice - kline.LowPrice;
                var highClose = Math.Abs(kline.HighPrice - prev.ClosePrice);
                var lowClose = Math.Abs(kline.LowPrice - prev.ClosePrice);
                var trueRange = Math.Max(highLow, Math.Max(highClose, lowClose));

                yield return trueRange;

                prev = kline;
            }
        }
    }
}