namespace Trader.Models
{
    public enum ExecutionType
    {
        None,
        New,
        Cancelled,
        Replaced,
        Rejected,
        Trade,
        Expired
    }
}