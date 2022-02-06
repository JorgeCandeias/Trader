namespace System.Collections.Generic;

public static class RateOfChangePercentExtensions
{
    public static IEnumerable<decimal> RateOfChangePercent(this IEnumerable<decimal> source, int length = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(length, 0, nameof(length));

        foreach (var item in source.RateOfChange(length))
        {
            yield return item * 100M;
        }
    }

    public static IEnumerable<decimal> RateOfChangePercent(this IEnumerable<Kline> source, int length = 9)
    {
        return source.RateOfChangePercent(x => x.ClosePrice, length);
    }

    public static IEnumerable<decimal> RateOfChangePercent<T>(this IEnumerable<T> source, Func<T, decimal> selector, int length = 9)
    {
        return source.Select(selector).RateOfChangePercent(length);
    }
}