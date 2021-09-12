using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoFactoryResolverServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoFactoryResolver(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAlgoFactoryResolver, AlgoFactoryResolver>();
        }
    }
}