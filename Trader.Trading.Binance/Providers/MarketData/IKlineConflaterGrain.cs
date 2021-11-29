using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Providers.MarketData
{
    internal interface IKlineConflaterGrain : IGrainWithStringKey
    {
        public ValueTask PushAsync(Kline item);
    }
}