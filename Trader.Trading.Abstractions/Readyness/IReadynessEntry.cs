using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Readyness
{
    public interface IReadynessEntry
    {
        Task<bool> IsReadyAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
    }
}