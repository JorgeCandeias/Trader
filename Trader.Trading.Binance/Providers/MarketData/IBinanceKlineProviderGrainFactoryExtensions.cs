using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Providers.MarketData;
using System;

namespace Orleans
{
    internal static class IBinanceKlineProviderGrainFactoryExtensions
    {
        public static IBinanceKlineProviderGrain GetBinanceKlineProviderGrain(this IGrainFactory factory, string symbol, KlineInterval interval)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return factory.GetGrain<IBinanceKlineProviderGrain>($"{symbol}|{interval}");
        }
    }
}