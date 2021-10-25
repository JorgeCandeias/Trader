using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory.MarketData
{
    public interface IInMemoryMarketDataStreamSender
    {
        IDisposable Register(Func<MarketDataStreamMessage, CancellationToken, Task> action);

        Task SendAsync(MarketDataStreamMessage message, CancellationToken cancellationToken = default);
    }
}