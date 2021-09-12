using Orleans;
using System;

namespace Outcompute.Trader.Trading.Watchdog
{
    internal class GrainWatchdogEntry : IGrainWatchdogEntry
    {
        private readonly Func<IGrainFactory, IGrain> _action;

        public GrainWatchdogEntry(Func<IGrainFactory, IGrain> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public IGrain GetGrain(IGrainFactory factory) => _action(factory);
    }
}