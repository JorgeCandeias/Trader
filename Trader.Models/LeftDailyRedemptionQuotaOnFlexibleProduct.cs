using Orleans.Concurrency;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record LeftDailyRedemptionQuotaOnFlexibleProduct(
        string Asset,
        decimal DailyQuota,
        decimal LeftQuota,
        decimal MinRedemptionAmount);
}