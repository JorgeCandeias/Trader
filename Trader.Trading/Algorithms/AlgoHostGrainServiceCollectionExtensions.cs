using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoHostGrainServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoHostGrain(this IServiceCollection services)
        {
            return services
                .ConfigureOptions<AlgoHostGrainOptionsConfigurator>()
                .AddOptions<AlgoHostGrainOptions>()
                .ValidateDataAnnotations()
                .Services;
        }
    }
}