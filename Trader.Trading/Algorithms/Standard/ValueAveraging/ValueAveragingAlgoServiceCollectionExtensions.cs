using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.ValueAveraging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ValueAveragingAlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddValueAveragingAlgoType(this IServiceCollection services)
        {
            return services
                .AddAlgoType<ValueAveragingAlgo>("ValueAveraging")
                .AddAlgoOptionsType<ValueAveragingAlgoOptions>();
        }

        public static IServiceCollection AddValueAveragingAlgo(this IServiceCollection services, string name, Action<AlgoOptions> configureAlgoOptions, Action<ValueAveragingAlgoOptions> configureUserOptions)
        {
            return services.AddAlgo("ValueAveraging", name, configureAlgoOptions, configureUserOptions);
        }
    }
}