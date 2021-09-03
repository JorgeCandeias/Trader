using System;

namespace Outcompute.Trader.Models
{
    public record Usage(
        RateLimitType Type,
        TimeSpan Window,
        int Count);
}