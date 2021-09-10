using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Watchdog
{
    internal class WatchdogGrainExtension : IWatchdogGrainExtension
    {
        public Task PingAsync() => Task.CompletedTask;
    }
}