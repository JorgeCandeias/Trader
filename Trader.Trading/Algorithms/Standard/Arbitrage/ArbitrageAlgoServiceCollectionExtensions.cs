using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Arbitrage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ArbitrageAlgoServiceCollectionExtensions
    {
        internal const string AlgoTypeName = "Arbitrage";

        internal static IServiceCollection AddArbitrageAlgoType(this IServiceCollection services)
        {
            return services
                .AddAlgoType<ArbitrageAlgo>(AlgoTypeName)
                .AddOptionsType<ArbitrageAlgoOptions>()
                .Services;
        }

        public static IAlgoBuilder<IAlgo, ArbitrageAlgoOptions> AddArbitrageAlgo(this IServiceCollection services, string name)
        {
            return services.AddAlgo<IAlgo, ArbitrageAlgoOptions>(name, AlgoTypeName);
        }
    }
}