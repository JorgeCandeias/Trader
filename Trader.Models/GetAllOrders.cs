using System;

namespace Trader.Models
{
    public record GetAllOrders(
        string Symbol,
        long? OrderId,
        DateTime? StartTime,
        DateTime? EndTime,
        int? Limit,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}