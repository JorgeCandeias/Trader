using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Arbitrage;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ArbitrageAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddArbitrageAlgo(this IServiceCollection services, Action<AlgoOptions> configureAlgoOptions, Action<ArbitrageAlgoOptions> configureUserOptions)
        {
            return services
                .AddAlgoType<ArbitrageAlgo, ArbitrageAlgoOptions>()
                .AddAlgo<ArbitrageAlgo, ArbitrageAlgoOptions>("Arbitrage", configureAlgoOptions, configureUserOptions);
        }
    }
}