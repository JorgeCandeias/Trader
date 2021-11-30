using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Tickers;

internal class TickerProvider : ITickerProvider
{
    private readonly IGrainFactory _factory;
    private readonly ITradingRepository _repository;

    public TickerProvider(IGrainFactory factory, ITradingRepository repository)
    {
        _factory = factory;
        _repository = repository;
    }

    public Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetTickerProviderReplicaGrain(symbol).TryGetTickerAsync();
    }

    public async Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(ticker, nameof(ticker));

        await _factory.GetTickerProviderReplicaGrain(ticker.Symbol).SetTickerAsync(ticker);
    }

    public ValueTask ConflateTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(ticker, nameof(ticker));

        return _factory.GetTickerConflaterGrain(ticker.Symbol).PushAsync(ticker);
    }
}