namespace Outcompute.Trader.Trading.Indicators;

public sealed class Subtract : Zip<decimal?, decimal?, decimal?>
{
    public Subtract(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, x => x.First - x.Second)
    {
    }
}