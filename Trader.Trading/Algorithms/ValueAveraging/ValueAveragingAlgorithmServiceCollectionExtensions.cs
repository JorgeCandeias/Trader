using System;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.ValueAveraging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ValueAveragingAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddValueAveragingAlgorithm(this IServiceCollection services, string name, Action<ValueAveragingAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, ValueAveragingAlgorithm>(sp => ActivatorUtilities.CreateInstance<ValueAveragingAlgorithm>(sp, name))
                .AddOptions<ValueAveragingAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}