using System;

namespace Outcompute.Trader.Models
{
    public record GetFlexibleProductPosition(
        string Asset,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}