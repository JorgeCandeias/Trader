using Outcompute.Trader.Trading.Algorithms;
using System;

namespace Orleans
{
    public static class IAlgoHostGrainFactoryExtensions
    {
        public static IAlgoHostGrain GetAlgoHostGrain(this IGrainFactory factory, string name)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            return factory.GetGrain<IAlgoHostGrain>(name);
        }
    }
}