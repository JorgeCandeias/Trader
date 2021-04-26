using System;

namespace Trader.Models
{
    public record Usage(
        RateLimitType Type,
        TimeSpan Window,
        int Count);
}