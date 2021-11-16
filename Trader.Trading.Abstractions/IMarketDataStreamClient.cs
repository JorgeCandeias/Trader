using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading;

public interface IMarketDataStreamClient : IDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task CloseAsync(CancellationToken cancellationToken = default);

    Task<MarketDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default);
}