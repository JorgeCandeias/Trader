namespace Outcompute.Trader.Trading.Indicators;

public enum AtrMethod
{
    Sma,
    Rma,
    Ema,
    Hma
}

public static class AverageTrueRangeExtensions
{
    public static IEnumerable<decimal?> AverageTrueRanges<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int periods = 14, AtrMethod method = AtrMethod.Rma)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));
        Guard.IsGreaterThanOrEqualTo(periods, 1, nameof(periods));

        var ranges = source.TrueRange(highSelector, lowSelector, closeSelector);

        return method switch
        {
            AtrMethod.Sma => ranges.Sma(periods),
            AtrMethod.Rma => ranges.Rma(periods),
            AtrMethod.Ema => ranges.Ema(periods),
            AtrMethod.Hma => ranges.HullMovingAverage(periods),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }

    public static IEnumerable<decimal?> AverageTrueRanges(this IEnumerable<Kline> source, int periods = 14, AtrMethod method = AtrMethod.Rma)
    {
        return source.AverageTrueRanges(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, periods, method);
    }
}