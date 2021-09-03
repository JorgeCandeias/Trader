using System;

namespace Outcompute.Trader.Models
{
    public record OrderQuery(
        string Symbol,
        long? OrderId,
        string? OriginalClientOrderId,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}