using System;
using Trader.Trading.Algorithms;
using Trader.Trading.Algorithms.Change;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ChangeAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddChangeAlgorithm(this IServiceCollection services, string name, Action<ChangeAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, ChangeAlgorithm>(sp => ActivatorUtilities.CreateInstance<ChangeAlgorithm>(sp, name))
                .AddOptions<ChangeAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}