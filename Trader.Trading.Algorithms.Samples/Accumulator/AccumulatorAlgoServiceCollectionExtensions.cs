using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Samples.Accumulator;

namespace Microsoft.Extensions.DependencyInjection;

public static class AccumulatorAlgoServiceCollectionExtensions
{
    internal const string AlgoTypeName = "Accumulator";

    public static IServiceCollection AddAccumulatorAlgoType(this IServiceCollection services)
    {
        return services
            .AddAlgoType<AccumulatorAlgo>(AlgoTypeName)
            .AddOptionsType<AccumulatorAlgoOptions>()
            .Services;
    }

    public static IAlgoBuilder<IAlgo, AccumulatorAlgoOptions> AddAccumulatorAlgo(this IServiceCollection services, string name)
    {
        return services.AddAlgo<IAlgo, AccumulatorAlgoOptions>(name, AlgoTypeName);
    }
}