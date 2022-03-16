namespace Outcompute.Trader.Trading.Indicators;

public record struct BullBearPower(decimal? BullPower, decimal? BearPower, decimal? Power);

public static class BullBearPowerExtensions
{
    public static IEnumerable<BullBearPower> BullBearPower<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int length = 13)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));
        Guard.IsGreaterThanOrEqualTo(length, 1, nameof(length));

        var high = source.Select(highSelector);
        var low = source.Select(lowSelector);
        var close = source.Select(closeSelector);
        var ema = close.Ema(length);

        var bull = high.Zip(ema, (x, y) => x - y);
        var bear = low.Zip(ema, (x, y) => x - y);
        var power = bull.Zip(bear, (x, y) => new BullBearPower(x, y, x + y));

        return power;
    }

    public static IEnumerable<BullBearPower> BullBearPower(this IEnumerable<Kline> source, int length = 13)
    {
        return source.BullBearPower(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, length);
    }
}