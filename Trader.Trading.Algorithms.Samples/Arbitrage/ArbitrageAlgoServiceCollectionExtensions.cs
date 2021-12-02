using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Samples.Arbitrage;

namespace Microsoft.Extensions.DependencyInjection;

public static class ArbitrageAlgoServiceCollectionExtensions
{
    internal const string AlgoTypeName = "Arbitrage";

    public static IServiceCollection AddArbitrageAlgoType(this IServiceCollection services)
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