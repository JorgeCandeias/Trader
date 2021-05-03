using System;
using Trader.Trading.Algorithms;
using Trader.Trading.Algorithms.Averaging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AveragingAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddAveragingAlgorithm(this IServiceCollection services, string name, Action<AveragingAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, AveragingAlgorithm>(sp => ActivatorUtilities.CreateInstance<AveragingAlgorithm>(sp, name))
                .AddOptions<AveragingAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}