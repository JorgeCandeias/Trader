using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators.Operators;

public sealed class Divide : Zip<decimal?, decimal?, decimal?>
{
    public Divide(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, (x, y) => MathN.SafeDiv(x, y))
    {
    }
}

public static partial class Indicator
{
    public static Divide Divide(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);
}