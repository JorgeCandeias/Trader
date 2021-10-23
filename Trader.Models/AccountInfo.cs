using Orleans.Concurrency;
using System;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record AccountInfo(
        decimal MakerCommission,
        decimal TakerCommission,
        decimal BuyerCommission,
        decimal SellerCommission,
        bool CanTrade,
        bool CanWithdraw,
        bool CanDeposit,
        DateTime UpdateTime,
        AccountType AccountType,
        ImmutableList<AccountBalance> Balances,
        ImmutableList<Permission> Permissions)
    {
        public static AccountInfo Empty { get; } = new AccountInfo(0, 0, 0, 0, false, false, false, DateTime.MinValue, AccountType.None, ImmutableList<AccountBalance>.Empty, ImmutableList<Permission>.Empty);
    }

    [Immutable]
    public record AccountBalance(
        string Asset,
        decimal Free,
        decimal Locked)
    {
        public static AccountBalance Empty { get; } = new AccountBalance(string.Empty, 0, 0);
    }
}