using Outcompute.Trader.Core.Randomizers;
using Outcompute.Trader.Core.Serializers;

namespace Microsoft.Extensions.DependencyInjection;

public static class TraderCoreServiceCollectionExtensions
{
    public static IServiceCollection AddTraderCoreServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IRandomGenerator, RandomGenerator>()
            .AddSingleton<IBase62NumberSerializer, Base62NumberSerializer>();
    }
}