using Outcompute.Trader.Trading.Binance.Providers.UserData;

namespace Orleans;

internal static class IBinanceUserDataReadynessGrainFactoryExtensions
{
    public static IBinanceUserDataReadynessGrain GetBinanceUserDataReadynessGrain(this IGrainFactory factory)
    {
        Guard.IsNotNull(factory, nameof(factory));

        return factory.GetGrain<IBinanceUserDataReadynessGrain>(Guid.Empty);
    }
}