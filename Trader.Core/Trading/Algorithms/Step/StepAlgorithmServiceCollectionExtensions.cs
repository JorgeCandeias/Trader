using System;
using Trader.Core.Trading.Algorithms;
using Trader.Core.Trading.Algorithms.Step;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StepAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddStepAlgorithm(this IServiceCollection services, string name, Action<StepAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, StepAlgorithm>(sp => ActivatorUtilities.CreateInstance<StepAlgorithm>(sp, name))
                .AddOptions<StepAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}