using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoHostGrainServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoHostGrain(this IServiceCollection services)
        {
            return services
                .ConfigureOptions<AlgoOptionsConfigurator>()
                .AddOptions<AlgoOptions>()
                .ValidateDataAnnotations()
                .Services;
        }
    }
}