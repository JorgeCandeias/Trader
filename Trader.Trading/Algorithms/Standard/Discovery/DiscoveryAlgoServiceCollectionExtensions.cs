using Outcompute.Trader.Trading.Algorithms.Standard.Discovery;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiscoveryAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscoveryAlgo(this IServiceCollection services)
        {
            return services
                .AddAlgoType<DiscoveryAlgo, DiscoveryAlgoOptions>()
                .AddAlgo<DiscoveryAlgo, DiscoveryAlgoOptions>("Discovery",
                    options =>
                    {
                        // noop
                    },
                    options =>
                    {
                        options.QuoteAssets.UnionWith(new[] { "BTC", "ETH", "BNB" });
                    });
        }
    }
}