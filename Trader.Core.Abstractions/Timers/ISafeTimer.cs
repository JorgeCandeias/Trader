using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trader.Core.Timers
{
    public interface ISafeTimer : IDisposable
    {
        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync(CancellationToken cancellationToken = default);
    }
}