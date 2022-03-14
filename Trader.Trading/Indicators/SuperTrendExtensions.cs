namespace System.Collections.Generic;

public enum SuperTrendDirection
{
    Down = -1,
    None = 0,
    Up = 1
}

public record SuperTrendValue
{
    public DateTime OpenTime { get; init; }
    public DateTime CloseTime { get; init; }
    public SuperTrendDirection Direction { get; init; }
    public decimal? High { get; init; }
    public decimal? Low { get; init; }
    public decimal? Midpoint { get; init; }
    public decimal? Close { get; init; }
    public decimal? Atr { get; init; }
}

public static class SuperTrendExtensions
{
    public static IEnumerable<SuperTrendValue> SuperTrend(this IEnumerable<Kline> source, int periods = 10, decimal multiplier = 3)
    {
        Guard.IsNotNull(source, nameof(source));

        using var sourceEnumerator = source.GetEnumerator();
        using var atrEnumerator = source.AverageTrueRanges(AtrSmoothing.Rma, periods).GetEnumerator();

        var direction = SuperTrendDirection.None;
        SuperTrendValue? prev = null;

        while (sourceEnumerator.MoveNext() && atrEnumerator.MoveNext())
        {
            var item = sourceEnumerator.Current;
            var atr = atrEnumerator.Current;

            var average = (item.HighPrice + item.LowPrice) / 2M;

            var spread = multiplier * atr;
            var high = average + spread;
            var low = average - spread;

            if (prev is not null)
            {
                high = (high < prev.High || prev.Close > prev.High) ? high : prev.High;

                low = (low > prev.Low || prev.Close < prev.Low) ? low : prev.Low;

                if (item.ClosePrice >= prev.High)
                {
                    direction = SuperTrendDirection.Up;
                }
                else if (item.ClosePrice <= prev.Low)
                {
                    direction = SuperTrendDirection.Down;
                }
            }

            prev = new SuperTrendValue
            {
                OpenTime = item.OpenTime,
                CloseTime = item.CloseTime,
                Direction = direction,
                High = high,
                Low = low,
                Midpoint = average,
                Close = item.ClosePrice,
                Atr = atr
            };

            yield return prev;
        }
    }
}