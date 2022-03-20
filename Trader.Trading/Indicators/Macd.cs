﻿namespace Outcompute.Trader.Trading.Indicators;

public record struct MacdResult(decimal? Macd, decimal? Signal, decimal? Histogram)
{
    public static MacdResult Empty => new();
}

public class Macd : CompositeIndicator<decimal?, MacdResult>
{
    internal const int DefaultFastPeriods = 12;
    internal const int DefaultSlowPeriods = 26;
    internal const int DefaultSignalPeriods = 9;

    public Macd(IndicatorResult<decimal?> source, int fastPeriods = DefaultFastPeriods, int slowPeriods = DefaultSlowPeriods, int signalPeriods = DefaultSignalPeriods)
        : base(source, x =>
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsGreaterThanOrEqualTo(fastPeriods, 1, nameof(fastPeriods));
            Guard.IsGreaterThanOrEqualTo(slowPeriods, 1, nameof(slowPeriods));
            Guard.IsGreaterThanOrEqualTo(signalPeriods, 1, nameof(signalPeriods));

            var fastEma = Indicator.Ema(source, fastPeriods);
            var slowEma = Indicator.Ema(source, slowPeriods);
            var macd = fastEma - slowEma;
            var signal = Indicator.Ema(macd, signalPeriods);
            var histogram = macd - signal;

            return Indicator.Zip(macd, signal, histogram, (m, s, h) => new MacdResult(m, s, h));
        })
    {
    }
}

public static partial class Indicator
{
    public static Macd Macd(this IndicatorResult<decimal?> source, int fastPeriods = Indicators.Macd.DefaultFastPeriods, int slowPeriods = Indicators.Macd.DefaultSlowPeriods, int signalPeriods = Indicators.Macd.DefaultSignalPeriods)
        => new(source, fastPeriods, slowPeriods, signalPeriods);

    public static IEnumerable<MacdResult> ToMacd(this IEnumerable<Kline> source, int fastPeriods = Indicators.Macd.DefaultFastPeriods, int slowPeriods = Indicators.Macd.DefaultSlowPeriods, int signalPeriods = Indicators.Macd.DefaultSignalPeriods)
        => source.Select(x => (decimal?)x.ClosePrice).Identity().Macd(fastPeriods, slowPeriods, signalPeriods);
}