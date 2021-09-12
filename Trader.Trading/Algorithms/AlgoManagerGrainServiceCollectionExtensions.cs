using Microsoft.Extensions.Configuration;
using Orleans;
using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoManagerGrainServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoManagerGrain(this IServiceCollection services, IConfigurationSection config)
        {
            return services
                .AddGrainWatchdogEntry(factory => factory.GetAlgoManagerGrain())
                .AddOptions<AlgoManagerGrainOptions>()
                .Bind(config)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}