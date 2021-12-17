using System.Runtime.InteropServices;

namespace System.Collections.Generic;

public enum SuperTrendDirection
{
    Down = -1,
    None = 0,
    Up = 1
}

[StructLayout(LayoutKind.Auto)]
public readonly record struct SuperTrendValue
{
    public SuperTrendDirection Direction { get; init; }
    public decimal High3 { get; init; }
    public decimal High2 { get; init; }
    public decimal High1 { get; init; }
    public decimal Low1 { get; init; }
    public decimal Low2 { get; init; }
    public decimal Low3 { get; init; }
    public decimal Close { get; init; }
}

public static class SuperTrendExtensions
{
    public static IEnumerable<SuperTrendValue> SuperTrend(this IEnumerable<Kline> source, int periods = 10, decimal multiplier1 = 3, decimal multiplier2 = 4, decimal multiplier3 = 5)
    {
        Guard.IsNotNull(source, nameof(source));

        using var sourceEnumerator = source.GetEnumerator();
        using var atrEnumerator = source.AverageTrueRange(periods).GetEnumerator();

        var direction = SuperTrendDirection.None;
        SuperTrendValue? prev = null;

        while (sourceEnumerator.MoveNext() && atrEnumerator.MoveNext())
        {
            var item = sourceEnumerator.Current;
            var atr = atrEnumerator.Current;

            var average = (item.HighPrice + item.LowPrice) / 2M;

            var spread1 = multiplier1 * atr;
            var spread2 = multiplier2 * atr;
            var spread3 = multiplier3 * atr;

            var high1 = average + spread1;
            var high2 = average + spread2;
            var high3 = average + spread3;

            var low1 = average - spread1;
            var low2 = average - spread2;
            var low3 = average - spread3;

            if (prev.HasValue)
            {
                high1 = (high1 < prev.Value.High1 || prev.Value.Close > prev.Value.High1) ? high1 : prev.Value.High1;
                high2 = (high2 < prev.Value.High2 || prev.Value.Close > prev.Value.High1) ? high2 : prev.Value.High2;
                high3 = (high3 < prev.Value.High3 || prev.Value.Close > prev.Value.High1) ? high3 : prev.Value.High3;

                low1 = (low1 > prev.Value.Low1 || prev.Value.Close < prev.Value.Low1) ? low1 : prev.Value.Low1;
                low2 = (low2 > prev.Value.Low2 || prev.Value.Close < prev.Value.Low1) ? low2 : prev.Value.Low2;
                low3 = (low3 > prev.Value.Low3 || prev.Value.Close < prev.Value.Low1) ? low3 : prev.Value.Low3;

                if (item.HighPrice >= prev.Value.High1)
                {
                    direction = SuperTrendDirection.Up;
                }
                else if (item.LowPrice <= prev.Value.Low1)
                {
                    direction = SuperTrendDirection.Down;
                }
            }

            prev = new SuperTrendValue
            {
                Direction = direction,
                High1 = high1,
                High2 = high2,
                High3 = high3,
                Low1 = low1,
                Low2 = low2,
                Low3 = low3,
                Close = item.ClosePrice
            };

            yield return prev.Value;
        }
    }
}