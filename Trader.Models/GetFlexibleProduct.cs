using System;

namespace Outcompute.Trader.Models
{
    public record GetFlexibleProduct(
        FlexibleProductStatus Status,
        FlexibleProductFeatured Featured,
        long? Current,
        long? Size,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}