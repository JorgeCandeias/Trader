namespace Outcompute.Trader.Trading.Indicators;

public sealed class Add : Zip<decimal?, decimal?, decimal?>
{
    public Add(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, x => x.First + x.Second)
    {
    }
}