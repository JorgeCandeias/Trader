namespace Outcompute.Trader.Trading;

public interface IMarketDataStreamClientFactory
{
    IMarketDataStreamClient Create(IReadOnlyCollection<string> streams);
}