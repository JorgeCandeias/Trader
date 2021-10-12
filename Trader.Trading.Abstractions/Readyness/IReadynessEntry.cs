using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Readyness
{
    public interface IReadynessEntry
    {
        ValueTask<bool> IsReadyAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
    }
}