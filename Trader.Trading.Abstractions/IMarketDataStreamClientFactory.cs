using System.Collections.Generic;

namespace Trader.Trading
{
    public interface IMarketDataStreamClientFactory
    {
        IMarketDataStreamClient Create(IReadOnlyCollection<string> streams);
    }
}