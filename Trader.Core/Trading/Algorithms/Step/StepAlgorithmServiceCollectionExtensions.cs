using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Trader.Core.Trading.Algorithms.Step;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StepAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddStepAlgorithm(this IServiceCollection services, IConfigurationSection section)
        {
            return services
                .AddSingleton<IStepAlgorithmFactory, StepAlgorithmFactory>()
                .AddSingleton<IConfigureOptions<StepAlgorithmOptions>, ConfigureStepAlgorithmOptions>(_ => new ConfigureStepAlgorithmOptions(section))
                .AddOptions<StepAlgorithmOptions>()
                .ValidateDataAnnotations()
                .Services;
        }
    }
}