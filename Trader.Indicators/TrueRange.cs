using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Indicators;

public class TrueRange : IndicatorBase<HLC, decimal?>
{
    private readonly bool _fallback;

    public TrueRange(IndicatorResult<HLC> source, bool fallback = false)
        : base(source, true)
    {
        _fallback = fallback;

        Ready();
    }

    protected override decimal? Calculate(int index)
    {
        var highLow = Source[index].High - Source[index].Low;

        var fallback = _fallback && (index < 1 || Source[index - 1].Close is null);
        if (fallback)
        {
            return highLow;
        }
        else if (index >= 1)
        {
            var highClose = MathN.Abs(Source[index].High - Source[index - 1].Close);
            var lowClose = MathN.Abs(Source[index].Low - Source[index - 1].Close);

            return MathN.Max(highLow, MathN.Max(highClose, lowClose));
        }
        else
        {
            return null;
        }
    }
}

public static partial class Indicator
{
    public static TrueRange TrueRange(this IndicatorResult<HLC> source, bool fallback = false)
        => new(source, fallback);

    public static IEnumerable<decimal?> ToTrueRange<T>(this IEnumerable<T> source, Func<T, decimal?> highSelector, Func<T, decimal?> lowSelector, Func<T, decimal?> closeSelector, bool fallback = false)
        => source.Select(x => new HLC(highSelector(x), lowSelector(x), closeSelector(x))).Identity().TrueRange(fallback);
}