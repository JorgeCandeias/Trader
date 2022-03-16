using Outcompute.Trader.Trading.Indicators;

namespace System.Collections.Generic;

public static class AwesomeOscillatorExtensions
{
    public static IEnumerable<decimal?> AwesomeOscillator<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, int fastLength = 5, int slowLength = 34)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsGreaterThanOrEqualTo(fastLength, 1, nameof(fastLength));
        Guard.IsGreaterThanOrEqualTo(slowLength, 1, nameof(slowLength));

        var hl2 = source.HL2(highSelector, lowSelector);
        var fast = hl2.Sma(fastLength);
        var slow = hl2.Sma(slowLength);

        return fast.Zip(slow, (f, s) => f - s);
    }

    public static IEnumerable<decimal?> AwesomeOscillator(this IEnumerable<Kline> source, int fastLength = 5, int slowLength = 34)
    {
        return source.AwesomeOscillator(x => x.HighPrice, x => x.LowPrice, fastLength, slowLength);
    }
}