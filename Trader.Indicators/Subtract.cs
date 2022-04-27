namespace Outcompute.Trader.Indicators;

public sealed class Subtract : Zip<decimal?, decimal?, decimal?>
{
    public Subtract(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, (x, y) => x - y)
    {
    }
}

public static partial class Indicator
{
    public static Subtract Subtract(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);
}