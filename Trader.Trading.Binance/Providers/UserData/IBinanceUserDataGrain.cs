using Orleans;

namespace Outcompute.Trader.Trading.Binance.Providers.UserData;

internal interface IBinanceUserDataGrain : IGrainWithGuidKey
{
    /// <summary>
    /// Returns <see cref="true"/> if the stream is synchronized, otherwise <see cref="false"/>.
    /// </summary>
    ValueTask<bool> IsReadyAsync();

    Task PingAsync();
}