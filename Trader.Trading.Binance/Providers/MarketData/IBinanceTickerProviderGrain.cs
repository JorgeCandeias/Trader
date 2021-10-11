using Orleans;
using Outcompute.Trader.Models;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IBinanceTickerProviderGrain : IGrainWithStringKey
    {
        public ValueTask<MiniTicker?> TryGetTickerAsync();
    }
}