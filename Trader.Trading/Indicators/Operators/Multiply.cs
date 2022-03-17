namespace Outcompute.Trader.Trading.Indicators.Operators;

public sealed class Multiply : Zip<decimal?, decimal?, decimal?>
{
    public Multiply(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) : base(first, second, (x, y) => x * y)
    {
    }
}

public static partial class Indicator
{
    public static Multiply Multiply(IIndicatorResult<decimal?> first, IIndicatorResult<decimal?> second) => new(first, second);
}