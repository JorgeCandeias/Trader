using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public sealed class Max : Zip<decimal?, decimal?, decimal?>
{
    public Max(IndicatorResult<decimal?> first, IndicatorResult<decimal?> second)
        : base(first, second, (x, y) => MathN.Max(x, y))
    {
    }
}

public static partial class Indicator
{
    public static Max Max(IndicatorResult<decimal?> first, IndicatorResult<decimal?> second)
        => new(first, second);
}