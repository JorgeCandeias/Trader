namespace Outcompute.Trader.Models;

public record RedeemFlexibleProduct(
    string ProductId,
    decimal Amount,
    SavingsRedemptionType Type,
    TimeSpan? ReceiveWindow,
    DateTime Timestamp);