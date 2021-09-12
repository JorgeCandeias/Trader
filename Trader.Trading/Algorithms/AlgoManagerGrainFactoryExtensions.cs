using Outcompute.Trader.Trading.Algorithms;
using System;

namespace Orleans
{
    public static class AlgoManagerGrainFactoryExtensions
    {
        public static IAlgoManagerGrain GetAlgoManagerGrain(this IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IAlgoManagerGrain>(Guid.Empty);
        }
    }
}