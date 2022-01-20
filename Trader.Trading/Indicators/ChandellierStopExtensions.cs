namespace System.Collections.Generic;

public enum ChandellierStopTrendDirection
{
    Unknown = 0,
    Up = 1,
    Down = 2
}

public record struct ChandellierValue
{
    public ChandellierStopTrendDirection Direction { get; init; }
    public decimal StopPrice { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Atr { get; init; }
}

public static class ChandellierStopExtensions
{
    public static IEnumerable<ChandellierValue> ChandellierStop(this IEnumerable<Kline> source, int atrPeriods = 14, int atrMultipler = 3)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(atrPeriods, 0, nameof(atrPeriods));
        Guard.IsGreaterThan(atrMultipler, 0, nameof(atrMultipler));

        var items = source.GetEnumerator();
        var atrs = source.AverageTrueRanges(atrPeriods).GetEnumerator();

        var direction = ChandellierStopTrendDirection.Unknown;

        if (items.MoveNext() && atrs.MoveNext())
        {
            var item = items.Current;
            var high = item.HighPrice;
            var low = item.LowPrice;

            var atr = atrs.Current;
            var sellPrice = high - atr * atrMultipler;
            var buyPrice = low + atr * atrMultipler;
            var stop = 0M;

            yield return new ChandellierValue
            {
                Direction = ChandellierStopTrendDirection.Unknown,
                StopPrice = stop,
                Atr = atrs.Current,
                High = high,
                Low = low
            };

            while (items.MoveNext() && atrs.MoveNext())
            {
                item = items.Current;
                atr = atrs.Current;

                // see if there was a break out
                if (item.ClosePrice <= sellPrice)
                {
                    direction = ChandellierStopTrendDirection.Down;
                }
                else if (item.ClosePrice >= buyPrice)
                {
                    direction = ChandellierStopTrendDirection.Up;
                }

                // if the trend is unknown the continue updating both sides until break out
                if (direction == ChandellierStopTrendDirection.Unknown)
                {
                    high = Math.Max(high, item.HighPrice);
                    low = Math.Min(low, item.LowPrice);

                    sellPrice = high - atr * atrMultipler;
                    buyPrice = low + atr * atrMultipler;
                    stop = 0M;
                }

                // if the trend is up then track the highs and reset lows
                else if (direction == ChandellierStopTrendDirection.Up)
                {
                    high = Math.Max(high, item.HighPrice);
                    low = item.LowPrice;

                    sellPrice = high - atr * atrMultipler;
                    buyPrice = low + atr * atrMultipler;

                    stop = sellPrice;
                }

                // if the trend is down then track the lows and reset the highs
                else if (direction == ChandellierStopTrendDirection.Down)
                {
                    high = item.HighPrice;
                    low = Math.Min(low, item.LowPrice);

                    sellPrice = high - atr * atrMultipler;
                    buyPrice = low + atr * atrMultipler;

                    stop = buyPrice;
                }

                yield return new ChandellierValue
                {
                    Direction = direction,
                    Atr = atr,
                    High = high,
                    Low = low,
                    StopPrice = stop
                };
            }
        }
    }
}