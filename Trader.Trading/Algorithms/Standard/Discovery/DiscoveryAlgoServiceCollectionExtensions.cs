using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Discovery;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiscoveryAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscoveryAlgo(this IServiceCollection services, Action<AlgoOptions> configureAlgoOptions, Action<DiscoveryAlgoOptions> configureUserOptions)
        {
            return services
                .AddAlgoType<DiscoveryAlgo, DiscoveryAlgoOptions>()
                .AddAlgo<DiscoveryAlgo, DiscoveryAlgoOptions>("Discovery", configureAlgoOptions, configureUserOptions);
        }
    }
}