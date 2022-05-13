namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal interface IMarketDataStreamer
{
    Task StartAsync(IEnumerable<string> tickers, IEnumerable<(string Symbol, KlineInterval Interval)> klines, CancellationToken cancellationToken = default);

    Task Completion { get; }
}