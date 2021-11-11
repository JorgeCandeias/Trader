using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Accumulator;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccumulatorAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddAccumulatorAlgo(this IServiceCollection services, Action<AlgoOptions> configureAlgoOptions, Action<AccumulatorAlgoOptions> configureUserOptions)
        {
            return services
                .AddAlgoType<AccumulatorAlgo, AccumulatorAlgoOptions>()
                .AddAlgo<AccumulatorAlgo, AccumulatorAlgoOptions>("Accumulator", configureAlgoOptions, configureUserOptions);
        }
    }
}