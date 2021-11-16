namespace Outcompute.Trader.Models;

public record GetLeftDailyRedemptionQuotaOnFlexibleProduct(
    string ProductId,
    SavingsRedemptionType Type,
    TimeSpan? ReceiveWindow,
    DateTime Timestamp);