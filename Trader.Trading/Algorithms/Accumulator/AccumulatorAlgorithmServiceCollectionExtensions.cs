using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Accumulator;
using System;

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