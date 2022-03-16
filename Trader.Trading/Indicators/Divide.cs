using Outcompute.Trader.Core.Mathematics;

namespace Outcompute.Trader.Trading.Indicators;

public sealed class Divide : Zip<decimal?, decimal?, decimal?>
{
    public Divide(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, x => MathN.SafeDiv(x.First, x.Second))
    {
    }
}