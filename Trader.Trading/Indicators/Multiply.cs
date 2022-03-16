namespace Outcompute.Trader.Trading.Indicators;

public sealed class Multiply : Zip<decimal?, decimal?, decimal?>
{
    public Multiply(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, x => x.First * x.Second)
    {
    }
}