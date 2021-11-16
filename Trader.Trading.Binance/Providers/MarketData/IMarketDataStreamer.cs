using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal interface IMarketDataStreamer
{
    Task StreamAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken = default);
}