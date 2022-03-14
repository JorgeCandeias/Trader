namespace System.Collections.Generic;

public enum AtrSmoothing
{
    Sma,
    Rma,
    Ema,
    Hma
}

public static class AverageTrueRangeExtensions
{
    public static IEnumerable<decimal?> AverageTrueRanges(this IEnumerable<Kline> source, AtrSmoothing smoothing = AtrSmoothing.Rma, int periods = 14)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsGreaterThan(periods, 0, nameof(periods));

        var ranges = source.TrueRanges();

        return smoothing switch
        {
            AtrSmoothing.Sma => ranges.SimpleMovingAverage(periods),
            AtrSmoothing.Rma => ranges.RunningMovingAverage(periods),
            AtrSmoothing.Ema => ranges.ExponentialMovingAverage(periods),
            AtrSmoothing.Hma => ranges.HullMovingAverage(periods),
            _ => throw new ArgumentOutOfRangeException(nameof(smoothing))
        };
    }
}