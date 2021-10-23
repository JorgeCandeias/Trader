using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Watchdog
{
    public interface IWatchdogEntry
    {
        Task ExecuteAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
    }
}