namespace Outcompute.Trader.Models
{
    public enum OrderStatus
    {
        None = 0,
        New = 1,
        PartiallyFilled = 2,
        Filled = 3,
        Canceled = 4,
        PendingCancel = 5,
        Rejected = 6,
        Expired = 7
    }

    public static class OrderStatusExtensions
    {
        public static bool IsCompletedStatus(this OrderStatus status)
        {
            return
                status == OrderStatus.Filled ||
                status == OrderStatus.Canceled ||
                status == OrderStatus.Rejected ||
                status == OrderStatus.Expired;
        }

        public static bool IsTransientStatus(this OrderStatus status)
        {
            return
                status == OrderStatus.New ||
                status == OrderStatus.PartiallyFilled ||
                status == OrderStatus.PendingCancel;
        }
    }
}