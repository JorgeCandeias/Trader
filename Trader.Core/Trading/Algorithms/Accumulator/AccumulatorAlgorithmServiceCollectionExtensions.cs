using System;
using Trader.Core.Trading.Algorithms;
using Trader.Core.Trading.Algorithms.Accumulator;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccumulatorAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddAccumulatorAlgorithm(this IServiceCollection services, string name, Action<AccumulatorAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, AccumulatorAlgorithm>(sp => ActivatorUtilities.CreateInstance<AccumulatorAlgorithm>(sp, name))
                .AddOptions<AccumulatorAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}