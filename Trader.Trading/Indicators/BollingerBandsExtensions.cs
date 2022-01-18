namespace System.Collections.Generic;

public static class BollingerBandsExtensions
{
    public static IEnumerable<BollingerBandValue> BollingerBands(this IEnumerable<Kline> source, Func<Kline, decimal> selector, int periods = 21, decimal multiplier = 2)
    {
        return source.Select(selector).BollingerBands(periods, multiplier);
    }

    public static IEnumerable<BollingerBandValue> BollingerBands(this IEnumerable<decimal> source, int periods = 21, decimal multiplier = 2)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        var averages = source.Sma(periods).GetEnumerator();
        var deviations = source.StdDev(periods).GetEnumerator();

        while (averages.MoveNext() && deviations.MoveNext())
        {
            yield return new BollingerBandValue
            {
                Average = averages.Current,
                High = averages.Current + deviations.Current * multiplier,
                Low = averages.Current - deviations.Current * multiplier
            };
        }
    }

    public record BollingerBandValue
    {
        public decimal Average { get; init; }
        public decimal High { get; init; }
        public decimal Low { get; init; }
    }
}