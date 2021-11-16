using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers;

public interface ITradeProvider
{
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
    Task<IReadOnlyList<AccountTrade>> GetTradesAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the trade with the specified parameters.
    /// </summary>
    Task<AccountTrade?> TryGetTradeAsync(string symbol, long tradeId, CancellationToken cancellationToken = default);
}