﻿using Orleans.Concurrency;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record FlexibleProductPosition(
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
        public static FlexibleProductPosition Zero(string asset) => new(asset, asset, asset, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, true);
    }
}