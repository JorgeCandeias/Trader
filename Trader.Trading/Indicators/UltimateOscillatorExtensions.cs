using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public static class UltimateOscillatorExtensions
{
    public static IEnumerable<decimal?> UltimateOscillator<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, int fastLength = 7, int mediumLength = 14, int slowLength = 28)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));
        Guard.IsGreaterThanOrEqualTo(fastLength, 1, nameof(fastLength));
        Guard.IsGreaterThanOrEqualTo(mediumLength, 1, nameof(mediumLength));
        Guard.IsGreaterThanOrEqualTo(slowLength, 1, nameof(slowLength));

        var paired = source.WithPrevious();
        var close = source.Select(closeSelector);
        var rangeHigh = paired.Select(x => MathN.Max(highSelector(x.Current), x.Previous != null ? closeSelector(x.Previous) : null));
        var rangeLow = paired.Select(x => MathN.Min(lowSelector(x.Current), x.Previous != null ? closeSelector(x.Previous) : null));
        var buyingPressure = close.Zip(rangeLow, (c, rl) => c - rl);
        var trueRange = rangeHigh.Zip(rangeLow, (rh, rl) => rh - rl);

        var bpFastSum = buyingPressure.MovingSum(fastLength).ToList();
        var bpMediumSum = buyingPressure.MovingSum(mediumLength).ToList();
        var bpSlowSum = buyingPressure.MovingSum(slowLength).ToList();

        var trFastSum = trueRange.MovingSum(fastLength);
        var trMediumSum = trueRange.MovingSum(mediumLength);
        var trSlowSum = trueRange.MovingSum(slowLength);

        var fastAverage = bpFastSum.Zip(trFastSum, (bp, tr) => MathN.SafeDiv(bp, tr));
        var mediumAverage = bpMediumSum.Zip(trMediumSum, (bp, tr) => MathN.SafeDiv(bp, tr));
        var slowAverage = bpSlowSum.Zip(trSlowSum, (bp, tr) => MathN.SafeDiv(bp, tr));

        return fastAverage.Zip(mediumAverage, slowAverage, (f, m, s) => 100 * (4 * f + 2 * m + s) / 7);
    }

    public static IEnumerable<decimal?> UltimateOscillator(this IEnumerable<Kline> source, int fastLength = 7, int mediumLength = 14, int slowLength = 28)
    {
        return source.UltimateOscillator(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice, fastLength, mediumLength, slowLength);
    }
}