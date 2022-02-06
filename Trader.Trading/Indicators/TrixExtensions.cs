namespace System.Collections.Generic;

public record TrixValue
{
    public decimal Ema1 { get; init; }
    public decimal Ema2 { get; init; }
    public decimal Ema3 { get; init; }
    public decimal RoC { get; init; }
    public decimal RoCP { get; init; }
}

public static class TrixExtensions
{
    public static IEnumerable<TrixValue> Trix(this IEnumerable<Kline> source, int periods = 9)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        var ema1 = source.Ema(periods).GetEnumerator();
        var ema2 = source.Ema(periods).Ema(periods).GetEnumerator();
        var ema3 = source.Ema(periods).Ema(periods).Ema(periods).GetEnumerator();
        var roc = source.Ema(periods).Ema(periods).Ema(periods).RateOfChange(1).GetEnumerator();
        var rocp = source.Ema(periods).Ema(periods).Ema(periods).RateOfChangePercent(1).GetEnumerator();

        while (ema1.MoveNext() && ema2.MoveNext() && ema3.MoveNext() && roc.MoveNext() && rocp.MoveNext())
        {
            yield return new TrixValue
            {
                Ema1 = ema1.Current,
                Ema2 = ema2.Current,
                Ema3 = ema3.Current,
                RoC = roc.Current,
                RoCP = rocp.Current
            };
        }
    }
}