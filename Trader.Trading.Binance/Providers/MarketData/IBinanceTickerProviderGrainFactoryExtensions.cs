using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using System;

namespace Orleans
{
    internal static class IBinanceTickerProviderGrainFactoryExtensions
    {
        public static IBinanceTickerProviderGrain GetBinanceTickerProviderGrain(this IGrainFactory factory, string symbol)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return factory.GetGrain<IBinanceTickerProviderGrain>(symbol);
        }
    }
}