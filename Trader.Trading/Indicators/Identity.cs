namespace Outcompute.Trader.Trading.Indicators;

public class Identity<T> : IndicatorBase<T, T>
{
    protected override T Calculate(int index)
    {
        return Source[index];
    }
}

public static partial class Indicator
{
    public static Identity<T> Identity<T>() => new();
}