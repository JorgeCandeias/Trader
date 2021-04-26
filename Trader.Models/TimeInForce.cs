namespace Trader.Models
{
    public enum TimeInForce
    {
        None = 0,
        GoodTillCanceled = 1,
        ImmediateOrCancel = 2,
        FillOrKill = 3
    }
}