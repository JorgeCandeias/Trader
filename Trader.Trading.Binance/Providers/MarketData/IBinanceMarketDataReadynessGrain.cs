using Orleans;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IBinanceMarketDataReadynessGrain : IGrainWithGuidKey
    {
        ValueTask<bool> IsReadyAsync();
    }
}