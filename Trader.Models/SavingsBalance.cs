using Orleans.Concurrency;

namespace Outcompute.Trader.Models;

[Immutable]
public record SavingsBalance(
    string Asset,
    string ProductId,
    string ProductName,
    decimal AnnualInterestRate,
    decimal AvgAnnualInterestRate,
    decimal DailyInterestRate,
    decimal FreeAmount,
    decimal FreezeAmount,
    decimal LockedAmount,
    decimal RedeemingAmount,
    decimal TodayPurchasedAmount,
    decimal TotalAmount,
    decimal TotalInterest,
    bool CanRedeem)
{
    public static SavingsBalance Empty { get; } = new(string.Empty, string.Empty, string.Empty, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, true);

    public static SavingsBalance Zero(string asset) => new(asset, asset, asset, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, true);
}