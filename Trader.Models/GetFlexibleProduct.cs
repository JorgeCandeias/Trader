using System;

namespace Outcompute.Trader.Models
{
    public record GetFlexibleProduct(
        SavingsStatus Status,
        SavingsFeatured Featured,
        long? Current,
        long? Size,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}