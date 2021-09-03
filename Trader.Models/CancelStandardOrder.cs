using System;

namespace Outcompute.Trader.Models
{
    public record CancelStandardOrder(
        string Symbol,
        long? OrderId,
        string? OriginalClientOrderId,
        string? NewClientOrderId,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}