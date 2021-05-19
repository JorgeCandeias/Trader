using System;

namespace Trader.Models
{
    public record GetKlines(
        string Symbol,
        KlineInterval Interval,
        DateTime StartTime,
        DateTime EndTime,
        int Limit);
}