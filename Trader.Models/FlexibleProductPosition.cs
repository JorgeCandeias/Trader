namespace Outcompute.Trader.Models
{
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
        decimal TotalInterest);
}