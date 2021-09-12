using Orleans;
using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoManagerGrainServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoManagerGrain(this IServiceCollection services)
        {
            return services
                .AddGrainWatchdogEntry(factory => factory.GetAlgoManagerGrain())
                .ConfigureOptions<AlgoManagerGrainOptionsConfigurator>()
                .AddOptions<AlgoManagerGrainOptions>()
                .ValidateDataAnnotations()
                .Services;
        }
    }
}