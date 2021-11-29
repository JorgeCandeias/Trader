using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;

namespace Outcompute.Trader.Trading.Providers.Trades;

internal class TradeProvider : ITradeProvider
{
    private readonly IGrainFactory _factory;
    private readonly ITradingRepository _repository;

    public TradeProvider(IGrainFactory factory, ITradingRepository repository)
    {
        _factory = factory;
        _repository = repository;
    }

    public Task SetLastSyncedTradeIdAsync(string symbol, long tradeId, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetTradeProviderGrain(symbol).SetLastSyncedTradeIdAsync(tradeId);
    }

    public Task<long> GetLastSyncedTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetTradeProviderGrain(symbol).GetLastSyncedTradeIdAsync();
    }

    public Task<TradeCollection> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetTradeProviderReplicaGrain(symbol).GetTradesAsync();
    }

    public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(trade, nameof(trade));

        return _factory.GetTradeProviderReplicaGrain(trade.Symbol).SetTradeAsync(trade);
    }

    public Task<AccountTrade?> TryGetTradeAsync(string symbol, long tradeId, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _factory.GetTradeProviderReplicaGrain(symbol).TryGetTradeAsync(tradeId);
    }

    public async Task SetTradesAsync(string symbol, IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));
        Guard.IsNotNull(trades, nameof(trades));

        await _repository
            .SetTradesAsync(trades, cancellationToken)
            .ConfigureAwait(false);

        await _factory
            .GetTradeProviderReplicaGrain(symbol)
            .SetTradesAsync(trades)
            .ConfigureAwait(false);
    }
}