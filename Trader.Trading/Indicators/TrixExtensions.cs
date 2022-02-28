namespace System.Collections.Generic;

public record TrixValue
{
    public decimal Value { get; init; }
    public decimal RoC { get; init; }
    public decimal RoC2 { get; init; }
}

public static class TrixExtensions
{
    public static IEnumerable<TrixValue> Trix(this IEnumerable<decimal> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        return source.Trix(x => x, periods);
    }

    public static IEnumerable<TrixValue> Trix(this IEnumerable<Kline> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        return source.Trix(x => x.ClosePrice, periods);
    }

    public static IEnumerable<TrixValue> Trix<T>(this IEnumerable<T> source, Func<T, decimal> selector, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));
        Guard.IsNotNull(selector, nameof(selector));

        var ema3 = source.Ema(selector, periods).Ema(periods).Ema(periods).GetEnumerator();
        var roc = source.Ema(selector, periods).Ema(periods).Ema(periods).RateOfChange(1).GetEnumerator();
        var roc2 = source.Ema(selector, periods).Ema(periods).Ema(periods).RateOfChange(1).RateOfChange(1).GetEnumerator();

        while (ema3.MoveNext() && roc.MoveNext() && roc2.MoveNext())
        {
            yield return new TrixValue
            {
                Value = ema3.Current,
                RoC = roc.Current * 10000,
                RoC2 = roc2.Current * 10000
            };
        }
    }
}