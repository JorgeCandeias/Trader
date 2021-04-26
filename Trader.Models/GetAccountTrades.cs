using System;

namespace Trader.Models
{
    public record GetAccountTrades(
        string Symbol,
        DateTime? StartTime,
        DateTime? EndTime,
        long? FromId,
        int? Limit,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}