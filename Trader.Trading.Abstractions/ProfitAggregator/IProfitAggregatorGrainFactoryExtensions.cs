using Outcompute.Trader.Trading.ProfitAggregator;
using System;

namespace Orleans
{
    public static class IProfitAggregatorGrainFactoryExtensions
    {
        public static IProfitAggregatorGrain GetProfitAggregatorGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IProfitAggregatorGrain>(Guid.Empty);
        }
    }
}