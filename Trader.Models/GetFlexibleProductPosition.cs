using System;

namespace Trader.Models
{
    public record GetFlexibleProductPosition(
        string Asset,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}