using Microsoft.Extensions.DependencyInjection;
using System;

namespace Trader.Core.Trading.Algorithms.BreakEven
{
    public static class BreakEvenAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddBreakEvenAlgorithm(this IServiceCollection services, string name, Action<BreakEvenAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, BreakEvenAlgorithm>(sp => ActivatorUtilities.CreateInstance<BreakEvenAlgorithm>(sp, name))
                .AddOptions<BreakEvenAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}