using Orleans.Concurrency;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record LeftDailyRedemptionQuotaOnFlexibleProduct(
        string Asset,
        decimal DailyQuota,
        decimal LeftQuota,
        decimal MinRedemptionAmount)
    {
        public static LeftDailyRedemptionQuotaOnFlexibleProduct Empty { get; } = new LeftDailyRedemptionQuotaOnFlexibleProduct(
            string.Empty,
            0m,
            0m,
            0m);
    }
}