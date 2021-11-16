using Outcompute.Trader.Core.Serializers;

namespace Microsoft.Extensions.DependencyInjection;

public static class Base62NumberSerializerServiceCollectionExtensions
{
    public static IServiceCollection AddBase62NumberSerializer(this IServiceCollection services)
    {
        return services.AddSingleton<IBase62NumberSerializer, Base62NumberSerializer>();
    }
}