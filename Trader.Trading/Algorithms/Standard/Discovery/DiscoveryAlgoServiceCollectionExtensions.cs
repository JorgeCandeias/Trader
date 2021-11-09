using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Discovery;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiscoveryAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscoveryAlgoType(this IServiceCollection services)
        {
            return services
                .AddAlgoType<DiscoveryAlgo, DiscoveryAlgoOptions>("Discovery");
        }

        public static IServiceCollection AddDiscoveryAlgo(this IServiceCollection services, Action<AlgoOptions> configureAlgoOptions, Action<DiscoveryAlgoOptions> configureUserOptions)
        {
            return services
                .AddDiscoveryAlgoType()
                .AddAlgo<DiscoveryAlgo, DiscoveryAlgoOptions>("Discovery", configureAlgoOptions, configureUserOptions);
        }
    }
}