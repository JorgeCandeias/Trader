using Outcompute.Trader.Trading.Binance.Providers.UserData;
using System;

namespace Orleans
{
    internal static class IBinanceUserDataGrainFactoryExtensions
    {
        public static IBinanceUserDataGrain GetBinanceUserDataGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IBinanceUserDataGrain>(Guid.Empty);
        }
    }
}