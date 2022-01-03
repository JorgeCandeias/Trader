namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal interface IMarketDataStreamClient : IDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task CloseAsync(CancellationToken cancellationToken = default);

    Task SubscribeAsync(long id, IEnumerable<string> streams, CancellationToken cancellationToken = default);

    Task<MarketDataStreamMessage> ReceiveAsync(CancellationToken cancellationToken = default);
}