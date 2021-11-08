using Outcompute.Trader.Trading.Providers.Savings;
using System;

namespace Orleans
{
    internal static class SavingsGrainFactoryExtensions
    {
        public static ISavingsGrain GetSavingsGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<ISavingsGrain>(Guid.Empty);
        }
    }
}