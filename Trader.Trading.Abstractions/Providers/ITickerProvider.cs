using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers;

public interface ITickerProvider
{
    Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default);

    Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default);

    ValueTask ConflateTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default);
}