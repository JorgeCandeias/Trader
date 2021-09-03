using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.MinimumBalance;
using System;

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