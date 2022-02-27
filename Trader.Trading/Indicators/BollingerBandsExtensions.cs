namespace System.Collections.Generic;

public static class BollingerBandsExtensions
{
    public static IEnumerable<BollingerBandValue> BollingerBands<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods = 21, decimal multiplier = 2)
    {
        return source.Select(selector).BollingerBands(periods, multiplier);
    }

    public static IEnumerable<BollingerBandValue> BollingerBands(this IEnumerable<Kline> source, int periods = 21, decimal multiplier = 2)
    {
        return source.Select(x => x.ClosePrice).BollingerBands(periods, multiplier);
    }

    public static IEnumerable<BollingerBandValue> BollingerBands(this IEnumerable<decimal> source, int periods = 21, decimal multiplier = 2)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(periods, 2, nameof(periods));

        var prices = source.GetEnumerator();
        var averages = source.Sma(periods).GetEnumerator();
        var deviations = source.StdDev(periods).GetEnumerator();

        while (prices.MoveNext() && averages.MoveNext() && deviations.MoveNext())
        {
            yield return new BollingerBandValue
            {
                Price = prices.Current,
                Average = averages.Current,
                High = averages.Current + deviations.Current * multiplier,
                Low = averages.Current - deviations.Current * multiplier
            };
        }
    }

    public record BollingerBandValue
    {
        public decimal Price { get; init; }
        public decimal Average { get; init; }
        public decimal High { get; init; }
        public decimal Low { get; init; }

        public bool IsHighOutlier => Price > High;
        public bool IsLowOutlier => Price < Low;
    }
}