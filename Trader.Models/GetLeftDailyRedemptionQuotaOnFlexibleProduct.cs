using System;

namespace Trader.Models
{
    public record GetLeftDailyRedemptionQuotaOnFlexibleProduct(
        string ProductId,
        FlexibleProductRedemptionType Type,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}