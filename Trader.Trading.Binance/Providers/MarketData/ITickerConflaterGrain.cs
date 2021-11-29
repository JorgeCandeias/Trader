using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface ITickerConflaterGrain : IGrainWithStringKey
    {
        public ValueTask PushAsync(MiniTicker item);
    }
}