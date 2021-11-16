using Orleans;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData;

internal interface IBinanceUserDataReadynessGrain : IGrainWithGuidKey
{
    ValueTask<bool> IsReadyAsync();
}