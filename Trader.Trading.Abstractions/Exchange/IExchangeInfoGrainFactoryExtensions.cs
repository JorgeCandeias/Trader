using Outcompute.Trader.Trading.Exchange;
using System;

namespace Orleans
{
    public static class IExchangeInfoGrainFactoryExtensions
    {
        public static IExchangeInfoGrain GetExchangeInfoGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IExchangeInfoGrain>(Guid.Empty);
        }
    }
}