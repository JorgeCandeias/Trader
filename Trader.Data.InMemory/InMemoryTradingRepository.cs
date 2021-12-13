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
        Guard.IsNotNull(symbol, nameof(symbol));

        return GetOrdersCoreAsync(symbol);
    }

    private async Task<IEnumerable<OrderQueryResult>> GetOrdersCoreAsync(string symbol)
    {
        return await _grain.GetOrdersAsync(symbol).ConfigureAwait(false);
    }

    public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(orders, nameof(orders));

        return _grain.SetOrdersAsync(orders.ToImmutableList());
    }

    public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(order, nameof(order));

        return _grain.SetOrderAsync(order);
    }

    #endregion Orders

    #region Klines

    public ValueTask<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return GetKlinesCoreAsync(symbol, interval, startOpenTime, endOpenTime);
    }

    private async ValueTask<IEnumerable<Kline>> GetKlinesCoreAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        var result = await _grain.GetKlinesAsync(symbol, interval).ConfigureAwait(false);

        // todo: move this filter into the grain
        return result
            .Where(x => x.OpenTime >= startOpenTime && x.OpenTime <= endOpenTime)
            .ToImmutableSortedSet(Kline.KeyComparer);
    }

    public ValueTask SetKlinesAsync(IEnumerable<Kline> items, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(items, nameof(items));

        return _grain.SetKlinesAsync(items.ToImmutableList());
    }

    public ValueTask SetKlineAsync(Kline item, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(item, nameof(item));

        return _grain.SetKlineAsync(item);
    }

    #endregion Klines

    #region Tickers

    public Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return _grain.TryGetTickerAsync(symbol);
    }

    public Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(ticker, nameof(ticker));

        return _grain.SetTickerAsync(ticker);
    }

    #endregion Tickers

    #region Trades

    public Task<IEnumerable<AccountTrade>> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return GetTradesCoreAsync(symbol);
    }

    private async Task<IEnumerable<AccountTrade>> GetTradesCoreAsync(string symbol)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        return await _grain.GetTradesAsync(symbol).ConfigureAwait(false);
    }

    public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(trade, nameof(trade));

        return _grain.SetTradeAsync(trade);
    }

    public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(trades, nameof(trades));

        return _grain.SetTradesAsync(trades.ToImmutableList());
    }

    #endregion Trades

    #region Balances

    public ValueTask SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(balances, nameof(balances));

        return _grain.SetBalancesAsync(balances.ToImmutableList());
    }

    public ValueTask<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(asset, nameof(asset));

        return _grain.TryGetBalanceAsync(asset);
    }

    public ValueTask<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion Balances
}