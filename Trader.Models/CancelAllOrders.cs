using System;

namespace Trader.Models
{
    public record CancelAllOrders(
        string Symbol,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}