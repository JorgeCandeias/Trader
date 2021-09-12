using System;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Step;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StepAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddStepAlgorithm(this IServiceCollection services, string name, Action<StepAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ISymbolAlgo, StepAlgorithm>(sp => ActivatorUtilities.CreateInstance<StepAlgorithm>(sp, name))
                .AddOptions<StepAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}