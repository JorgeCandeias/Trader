using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public class TrueRange : IndicatorBase<HLC, decimal?>
{
    public TrueRange()
    {
    }

    public TrueRange(IIndicatorResult<HLC> source) : this()
    {
        Guard.IsNotNull(source, nameof(source));

        LinkFrom(source);
    }

    protected override decimal? Calculate(int index)
    {
        if (index == 0)
        {
            return Source[0].High - Source[0].Low;
        }

        var highLow = Source[index].High - Source[index].Low;
        var highClose = MathN.Abs(Source[index].High - Source[index - 1].Close);
        var lowClose = MathN.Abs(Source[index].Low - Source[index - 1].Close);

        return MathN.Max(highLow, MathN.Max(highClose, lowClose));
    }
}

public static class TrueRangeEnumerableExtensions
{
    public static IEnumerable<decimal?> TrueRange<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector)
    {
        Guard.IsNotNull(source, nameof(source));
        Guard.IsNotNull(highSelector, nameof(highSelector));
        Guard.IsNotNull(lowSelector, nameof(lowSelector));
        Guard.IsNotNull(closeSelector, nameof(closeSelector));

        using var indicator = new TrueRange();

        foreach (var item in source)
        {
            var high = highSelector(item);
            var low = lowSelector(item);
            var close = closeSelector(item);

            indicator.Add(new HLC(high, low, close));

            yield return indicator[^1];
        }
    }

    public static IEnumerable<decimal?> TrueRange(this IEnumerable<Kline> source)
    {
        return source.TrueRange(x => x.HighPrice, x => x.LowPrice, x => x.ClosePrice);
    }
}