using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class TrueRangeIndicator : IndicatorBase<(decimal? High, decimal? Low, decimal? Close), decimal?>
{
    protected override decimal? Calculate(int index)
    {
        if (index == 0)
        {
            return Source[0].High - Source[0].Low;
        }

        var highLow = Source[^1].High - Source[^1].Low;
        var highClose = MathN.Abs(Source[^1].High - Source[^2].Close);
        var lowClose = MathN.Abs(Source[^1].Low - Source[^2].Close);
        var trueRange = MathN.Max(highLow, MathN.Max(highClose, lowClose));

        return trueRange;
    }
}

public static class TrueRangeIndicatorEnumerableExtensions
{
    public static IEnumerable<decimal?> TrueRanges(this IEnumerable<(decimal? High, decimal? Low, decimal? Close)> source)
    {
        Guard.IsNotNull(source, nameof(source));

        var indicator = new TrueRangeIndicator();

        foreach (var (high, low, close) in source)
        {
            indicator.Add((high, low, close));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> TrueRanges<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector)
    {
        return source.Select(x => (highSelector(x), lowSelector(x), closeSelector(x))).TrueRanges();
    }

    public static IEnumerable<decimal?> TrueRanges(this IEnumerable<Kline> source)
    {
        return source.TrueRanges(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice);
    }
}