using System;
using Trader.Trading.Algorithms;
using Trader.Trading.Algorithms.MinimumBalance;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MinimumBalanceAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddMinimumBalanceAlgorithm(this IServiceCollection services, string name, Action<MinimumBalanceAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, MinimumBalanceAlgorithm>(sp => ActivatorUtilities.CreateInstance<MinimumBalanceAlgorithm>(sp, name))
                .AddOptions<MinimumBalanceAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}