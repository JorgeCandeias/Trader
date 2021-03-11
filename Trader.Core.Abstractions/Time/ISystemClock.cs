using System;

namespace Trader.Core.Time
{
    public interface ISystemClock
    {
        DateTime UtcNow { get; }
    }
}