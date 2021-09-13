using Outcompute.Trader.Trading.ProfitAggregator;
using System;

namespace Orleans
{
    public static class IProfitAggregatorLocalGrainFactoryExtensions
    {
        public static IProfitAggregatorLocalGrain GetProfitAggregatorLocalGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IProfitAggregatorLocalGrain>(Guid.Empty);
        }
    }
}