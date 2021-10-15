using Orleans.Concurrency;

namespace Outcompute.Trader.Models
{
    // todo: re-order these fields
    [Immutable]
    public record FlexibleProductPosition(
        decimal AnnualInterestRate,
        string Asset,
        decimal AvgAnnualInterestRate,
        bool CanRedeem,
        decimal DailyInterestRate,
        decimal FreeAmount,
        decimal FreezeAmount,
        decimal LockedAmount,
        string ProductId,
        string ProductName,
        decimal RedeemingAmount,
        decimal TodayPurchasedAmount,
        decimal TotalAmount,
        decimal TotalInterest)
    {
        public static FlexibleProductPosition Zero(string asset) => new(0m, asset, 0m, true, 0m, 0m, 0m, 0m, asset, asset, 0m, 0m, 0m, 0m);
    }
}