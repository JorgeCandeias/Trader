using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Data.InMemory;

internal class InMemoryTradingRepository : ITradingRepository
{
    private readonly IInMemoryTradingRepositoryGrain _grain;

    public InMemoryTradingRepository(IGrainFactory factory)
    {
        _grain = factory.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty);
    }

    #region Orders

    public Task<IEnumerable<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return GetOrdersCoreAsync(symbol);
    }

    private async Task<IEnumerable<OrderQueryResult>> GetOrdersCoreAsync(string symbol)
    {
        return await _grain.GetOrdersAsync(symbol).ConfigureAwait(false);
    }

    public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
    {
        if (orders is null) throw new ArgumentNullException(nameof(orders));

        return _grain.SetOrdersAsync(orders.ToImmutableList());
    }

    public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
    {
        if (order is null) throw new ArgumentNullException(nameof(order));

        return _grain.SetOrderAsync(order);
    }

    #endregion Orders

    #region Klines

    public ValueTask<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return GetKlinesCoreAsync(symbol, interval, startOpenTime, endOpenTime);
    }

    private async ValueTask<IEnumerable<Kline>> GetKlinesCoreAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime)
    {
        var result = await _grain.GetKlinesAsync(symbol, interval).ConfigureAwait(false);

        return result
            .Where(x => x.OpenTime >= startOpenTime && x.OpenTime <= endOpenTime)
            .ToImmutableSortedSet(KlineComparer.Key);
    }

    public ValueTask SetKlinesAsync(IEnumerable<Kline> items, CancellationToken cancellationToken = default)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));

        return _grain.SetKlinesAsync(items.ToImmutableList());
    }

    public ValueTask SetKlineAsync(Kline item, CancellationToken cancellationToken = default)
    {
        return _grain.SetKlineAsync(item);
    }

    #endregion Klines

    #region Tickers

    public Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return _grain.TryGetTickerAsync(symbol);
    }

    public Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default)
    {
        return _grain.SetTickerAsync(ticker);
    }

    #endregion Tickers

    #region Trades

    public Task<IEnumerable<AccountTrade>> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));

        return GetTradesCoreAsync(symbol);
    }

    private async Task<IEnumerable<AccountTrade>> GetTradesCoreAsync(string symbol)
    {
        return await _grain.GetTradesAsync(symbol).ConfigureAwait(false);
    }

    public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
    {
        return _grain.SetTradeAsync(trade);
    }

    public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
    {
        if (trades is null) throw new ArgumentNullException(nameof(trades));

        return _grain.SetTradesAsync(trades.ToImmutableList());
    }

    #endregion Trades

    #region Balances

    public ValueTask SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
    {
        if (balances is null) throw new ArgumentNullException(nameof(balances));

        return _grain.SetBalancesAsync(balances.ToImmutableList());
    }

    public ValueTask<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
    {
        return _grain.TryGetBalanceAsync(asset);
    }

    public ValueTask<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion Balances
}