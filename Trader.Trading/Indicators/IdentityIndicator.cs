namespace Outcompute.Trader.Trading.Indicators;

public class IdentityIndicator<T> : IndicatorBase<T, T>
{
    protected override T Calculate(int index)
    {
        return Source[index];
    }
}