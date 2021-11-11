using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Discovery;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiscoveryAlgoServiceCollectionExtensions
    {
        internal const string AlgoTypeName = "Discovery";

        internal static IServiceCollection AddDiscoveryAlgoType(this IServiceCollection services)
        {
            return services
                .AddAlgoType<DiscoveryAlgo>(AlgoTypeName)
                .AddOptionsType<DiscoveryAlgoOptions>()
                .Services;
        }

        public static IAlgoBuilder AddDiscoveryAlgo(this IServiceCollection services, string name)
        {
            return services.AddAlgo(name, AlgoTypeName);
        }
    }
}