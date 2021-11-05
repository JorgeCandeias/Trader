using System;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Providers.Swap
{
    public class SwapPoolOptions
    {
        public bool AutoAddEnabled { get; set; }

        public ISet<string> IsolatedAssets { get; } = new HashSet<string>();

        public ISet<string> ExcludedAssets { get; } = new HashSet<string>();

        public TimeSpan PoolCooldown { get; set; } = TimeSpan.FromMinutes(1);
    }
}