using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Streams.MarketData
{
    internal interface IBinanceMarketDataGrain : IGrainWithGuidKey
    {
        Task<MiniTicker?> TryGetTickerAsync(string symbol);
    }
}