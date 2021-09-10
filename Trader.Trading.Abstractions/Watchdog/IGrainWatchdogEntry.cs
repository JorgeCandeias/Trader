using Orleans;

namespace Outcompute.Trader.Trading.Watchdog
{
    public interface IGrainWatchdogEntry
    {
        IGrain GetGrain();
    }
}