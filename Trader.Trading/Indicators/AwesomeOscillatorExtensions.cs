namespace System.Collections.Generic;

public static class AwesomeOscillatorExtensions
{
    public static IEnumerable<decimal?> AwesomeOscillator(this IEnumerable<(decimal? high, decimal? low)> source, int fastLength = 5, int slowLength = 34)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThanOrEqualTo(fastLength, 1, nameof(fastLength));
        Guard.IsGreaterThanOrEqualTo(slowLength, 1, nameof(slowLength));

        var fast = source.HL2().SimpleMovingAverage(fastLength).GetEnumerator();
        var slow = source.HL2().SimpleMovingAverage(slowLength).GetEnumerator();

        while (fast.MoveNext() && slow.MoveNext())
        {
            yield return fast.Current - slow.Current;
        }
    }

    public static IEnumerable<decimal?> AwesomeOscillator(this IEnumerable<Kline> source, int fastLength = 5, int slowLength = 34)
    {
        return source
            .Select(x => ((decimal?)x.HighPrice, (decimal?)x.LowPrice))
            .AwesomeOscillator(fastLength, slowLength);
    }
}