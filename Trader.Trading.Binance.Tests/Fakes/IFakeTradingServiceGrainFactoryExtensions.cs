using Outcompute.Trader.Trading.Binance.Tests.Fakes;
using System;

namespace Orleans
{
    internal static class IFakeTradingServiceGrainFactoryExtensions
    {
        public static IFakeTradingServiceGrain GetFakeTradingServiceGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IFakeTradingServiceGrain>(Guid.Empty);
        }
    }
}