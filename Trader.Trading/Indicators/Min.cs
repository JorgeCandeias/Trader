using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public sealed class Min : Zip<decimal?, decimal?, decimal?>
{
    public Min(IndicatorResult<decimal?> first, IndicatorResult<decimal?> second)
        : base(first, second, (x, y) => MathN.Min(x, y))
    {
    }
}

public static partial class Indicator
{
    public static Min Min(IndicatorResult<decimal?> first, IndicatorResult<decimal?> second)
        => new(first, second);
}