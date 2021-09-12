using System;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.TimeAveraging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TimeAveragingAlgorithmServiceCollectionExtensions
    {
        public static IServiceCollection AddTimeAveragingAlgorithm(this IServiceCollection services, string name, Action<TimeAveragingAlgorithmOptions> configure)
        {
            return services
                .AddSingleton<ISymbolAlgo, TimeAveragingAlgorithm>(sp => ActivatorUtilities.CreateInstance<TimeAveragingAlgorithm>(sp, name))
                .AddOptions<TimeAveragingAlgorithmOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}