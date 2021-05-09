using System;
using Trader.Trading.Algorithms;
using Trader.Trading.Algorithms.TimeAveraging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TimeAveragingAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddTimeAveragingAlgorithm(this IServiceCollection services, string name, Action<TimeAveragingAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ITradingAlgorithm, TimeAveragingAlgorithm>(sp => ActivatorUtilities.CreateInstance<TimeAveragingAlgorithm>(sp, name))
                .AddOptions<TimeAveragingAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}