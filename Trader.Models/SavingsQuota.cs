using Orleans.Concurrency;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record SavingsQuota(
        string Asset,
        decimal DailyQuota,
        decimal LeftQuota,
        decimal MinRedemptionAmount)
    {
        public static SavingsQuota Empty { get; } = new SavingsQuota(string.Empty, 0m, 0m, 0m);

        public static SavingsQuota Zero(string asset) => new(asset, 0m, 0m, 0m);
    }
}