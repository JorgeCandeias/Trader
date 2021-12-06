namespace Outcompute.Trader.Trading.Providers;

public interface ITradeProvider
{
    /// <summary>
    /// Sets the last synced trade id.
    /// </summary>
    Task SetLastSyncedTradeIdAsync(string symbol, long tradeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last synced trade id.
    /// </summary>
    Task<long> GetLastSyncedTradeIdAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the specified trade.
    /// </summary>
    Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishers the specified trades.
    /// </summary>
    Task SetTradesAsync(string symbol, IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all trades for the specified symbol.
    /// </summary>
    Task<ImmutableSortedSet<AccountTrade>> GetTradesAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the trade with the specified parameters.
    /// </summary>
    Task<AccountTrade?> TryGetTradeAsync(string symbol, long tradeId, CancellationToken cancellationToken = default);
}