namespace Outcompute.Trader.Trading.Providers.Klines;

internal interface IKlineConflaterGrain : IGrainWithStringKey
{
    public ValueTask PushAsync(Kline item);
}