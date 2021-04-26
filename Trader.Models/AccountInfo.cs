using System;
using System.Collections.Immutable;

namespace Trader.Models
{
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
        ImmutableList<Permission> Permissions);

    public record AccountBalance(
        string Asset,
        decimal Free,
        decimal Locked);
}