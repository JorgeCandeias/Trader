namespace Outcompute.Trader.Trading.Providers.Tickers;

internal interface ITickerConflaterGrain : IGrainWithStringKey
{
    ValueTask PushAsync(MiniTicker item);
}