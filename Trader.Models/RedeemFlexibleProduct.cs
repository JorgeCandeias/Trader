using System;

namespace Trader.Models
{
    public record RedeemFlexibleProduct(
        string ProductId,
        decimal Amount,
        FlexibleProductRedemptionType Type,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}