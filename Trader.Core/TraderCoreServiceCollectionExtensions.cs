using Outcompute.Trader.Core.Randomizers;

namespace Microsoft.Extensions.DependencyInjection;

public static class TraderCoreServiceCollectionExtensions
{
    public static IServiceCollection AddTraderCoreServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IRandomGenerator, RandomGenerator>();
    }
}