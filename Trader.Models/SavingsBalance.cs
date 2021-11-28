using Orleans.Concurrency;

namespace Outcompute.Trader.Models;

/// <summary>
/// Savings balance information for an asset.
/// </summary>
/// <param name="Asset">The asset this balance refers to.</param>
/// <param name="ProductId">The id of the underlying savings product.</param>
/// <param name="ProductName">The name of the underlying savings product.</param>
/// <param name="AnnualInterestRate">The annual interest rate of the savings product.</param>
/// <param name="AvgAnnualInterestRate">The average annual interest rate of the savings product.</param>
/// <param name="DailyInterestRate">The daily interest rate of the savings product.</param>
/// <param name="FreeAmount">The free balance in savings.</param>
/// <param name="FreezeAmount">The freezed balance in savings.</param>
/// <param name="LockedAmount">The locked balance in savings.</param>
/// <param name="RedeemingAmount">The balance being redeemed in savings.</param>
/// <param name="TodayPurchasedAmount">The amount purchased today.</param>
/// <param name="TotalAmount">The total amount in savings.</param>
/// <param name="TotalInterest">The total interest received.</param>
/// <param name="CanRedeem">Whether the savings product can be redeemed at this time.</param>
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
    /// <summary>
    /// Gets a zero <see cref="SavingsBalance"/> instance with no asset.
    /// </summary>
    public static SavingsBalance Empty { get; } = new(string.Empty, string.Empty, string.Empty, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, true);

    /// <summary>
    /// Gets a zero <see cref="SavingsBalance"/> instance with the specified asset.
    /// </summary>
    /// <param name="asset"></param>
    /// <returns></returns>
    public static SavingsBalance Zero(string asset) => new(asset, asset, asset, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, true);
}