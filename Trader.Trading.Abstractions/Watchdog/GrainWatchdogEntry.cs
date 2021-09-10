using Orleans;
using System;

namespace Outcompute.Trader.Trading.Watchdog
{
    internal class GrainWatchdogEntry : IGrainWatchdogEntry
    {
        private readonly Func<IGrain> _factory;

        public GrainWatchdogEntry(Func<IGrain> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public IGrain GetGrain() => _factory();
    }
}