using System;

namespace Trader.Models
{
    public record GetAccountInfo(
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}