using Outcompute.Trader.Trading.Binance.Providers.Savings;
using System;

namespace Orleans
{
    internal static class IBinanceSavingsGrainFactoryExtensions
    {
        public static IBinanceSavingsGrain GetBinanceSavingsGrain(this IGrainFactory factory, string asset)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return factory.GetGrain<IBinanceSavingsGrain>(asset);
        }
    }
}