using System;

namespace Outcompute.Trader.Core.Time
{
    public class SystemClock : ISystemClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}