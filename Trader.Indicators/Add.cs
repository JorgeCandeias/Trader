namespace Outcompute.Trader.Indicators;

public sealed class Add : Zip<decimal?, decimal?, decimal?>
{
    public Add(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, (x, y) => x + y)
    {
    }
}

public static partial class Indicator
{
    public static Add Add(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);
}