using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.ValueAveraging;

namespace Microsoft.Extensions.DependencyInjection;

public static class ValueAveragingAlgoServiceCollectionExtensions
{
    internal const string AlgoTypeName = "ValueAveraging";

    internal static IServiceCollection AddValueAveragingAlgoType(this IServiceCollection services)
    {
        return services
            .AddAlgoType<ValueAveragingAlgo>(AlgoTypeName)
            .AddOptionsType<ValueAveragingAlgoOptions>()
            .Services;
    }

    public static IAlgoBuilder<IAlgo, ValueAveragingAlgoOptions> AddValueAveragingAlgo(this IServiceCollection services, string name)
    {
        return services.AddAlgo<IAlgo, ValueAveragingAlgoOptions>(name, AlgoTypeName);
    }
}