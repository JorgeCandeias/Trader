using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Watchdog
{
    internal class GrainWatchdog : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}