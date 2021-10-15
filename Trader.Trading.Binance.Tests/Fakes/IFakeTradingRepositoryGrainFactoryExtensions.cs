using Outcompute.Trader.Trading.Binance.Tests.Fakes;
using System;

namespace Orleans
{
    internal static class IFakeTradingRepositoryGrainFactoryExtensions
    {
        public static IFakeTradingRepositoryGrain GetFakeTradingRepositoryGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IFakeTradingRepositoryGrain>(Guid.Empty);
        }
    }
}