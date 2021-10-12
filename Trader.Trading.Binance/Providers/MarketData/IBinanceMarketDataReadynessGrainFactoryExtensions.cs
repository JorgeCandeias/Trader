using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using System;

namespace Orleans
{
    internal static class IBinanceMarketDataReadynessGrainFactoryExtensions
    {
        public static IBinanceMarketDataReadynessGrain GetBinanceMarketDataReadynessGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IBinanceMarketDataReadynessGrain>(Guid.Empty);
        }
    }
}