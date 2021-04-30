using System;

namespace Trader.Models
{
    public record Balance(
        string Asset,
        decimal Free,
        decimal Locked,
        DateTime UpdatedTime);
}