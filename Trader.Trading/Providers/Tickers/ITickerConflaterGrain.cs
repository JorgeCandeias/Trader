using Orleans;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Providers.Tickers;

internal interface ITickerConflaterGrain : IGrainWithStringKey
{
    public ValueTask PushAsync(MiniTicker item);
}