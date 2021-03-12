using Microsoft.Extensions.DependencyInjection;
using System;

namespace Trader.Core.Trading.Algorithms.Accumulator
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