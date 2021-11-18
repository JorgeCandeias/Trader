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

    public ValueTask<TradeCollection> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return _factory.GetTradeProviderReplicaGrain(symbol).GetTradesAsync();
    }

    public ValueTask SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
    {
        if (trade is null) throw new ArgumentNullException(nameof(trade));

        return SetTradeCoreAsync(trade, cancellationToken);
    }

    private async ValueTask SetTradeCoreAsync(AccountTrade trade, CancellationToken cancellationToken = default)
    {
        await _repository
            .SetTradeAsync(trade, cancellationToken)
            .ConfigureAwait(false);

        await _factory
            .GetTradeProviderReplicaGrain(trade.Symbol)
            .SetTradeAsync(trade);
    }

    public ValueTask<AccountTrade?> TryGetTradeAsync(string symbol, long tradeId, CancellationToken cancellationToken = default)
    {
        return _factory.GetTradeProviderReplicaGrain(symbol).TryGetTradeAsync(tradeId);
    }

    public ValueTask SetTradesAsync(string symbol, IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        if (trades is null) throw new ArgumentNullException(nameof(trades));

        foreach (var item in trades)
        {
            if (item.Symbol != symbol) throw new ArgumentOutOfRangeException(nameof(trades), $"Order has symbol '{item.Symbol}' different from partition symbol '{symbol}'");
        }

        return SetTradesCoreAsync(symbol, trades, cancellationToken);
    }

    private async ValueTask SetTradesCoreAsync(string symbol, IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
    {
        await _repository
            .SetTradesAsync(trades, cancellationToken)
            .ConfigureAwait(false);

        await _factory
            .GetTradeProviderReplicaGrain(symbol)
            .SetTradesAsync(trades)
            .ConfigureAwait(false);
    }
}