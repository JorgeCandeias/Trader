namespace Outcompute.Trader.Trading.Binance.Providers.MarketData;

internal interface IMarketDataStreamClientFactory
{
    IMarketDataStreamClient Create(IReadOnlyCollection<string> streams);
}