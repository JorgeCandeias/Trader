using Orleans;
using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoManagerGrainServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoManagerGrain(this IServiceCollection services)
        {
            return services
                .AddWatchdogEntry((sp, ct) => sp.GetRequiredService<IGrainFactory>().GetAlgoManagerGrain().PingAsync())
                .ConfigureOptions<AlgoManagerGrainOptionsConfigurator>()
                .AddOptions<AlgoManagerGrainOptions>()
                .ValidateDataAnnotations()
                .Services;
        }
    }
}