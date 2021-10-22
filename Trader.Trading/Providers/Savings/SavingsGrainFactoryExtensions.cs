using Outcompute.Trader.Trading.Providers.Savings;
using System;

namespace Orleans
{
    internal static class SavingsGrainFactoryExtensions
    {
        public static ISavingsGrain GetSavingsGrain(this IGrainFactory factory, string asset)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            return factory.GetGrain<ISavingsGrain>(asset);
        }
    }
}