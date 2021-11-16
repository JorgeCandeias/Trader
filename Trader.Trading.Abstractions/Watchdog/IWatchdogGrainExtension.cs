using Orleans.Runtime;

namespace Outcompute.Trader.Trading.Watchdog;

public interface IWatchdogGrainExtension : IGrainExtension
{
    Task PingAsync();
}