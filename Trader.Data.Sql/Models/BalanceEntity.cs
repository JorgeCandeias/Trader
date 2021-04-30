using System;

namespace Trader.Data.Sql.Models
{
    internal record BalanceEntity(
        string Asset,
        decimal Free,
        decimal Locked,
        DateTime UpdatedTime);
}