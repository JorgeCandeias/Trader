using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    // todo: add attribute-based auto-discovery in addition to these methods
    public static class AlgoFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoType<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            return services.AddNamedSingleton<IAlgoFactory, AlgoFactory<TAlgo>>(typeName);
        }

        public static IServiceCollection AddAlgoType<TAlgo, TOptions>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
            where TOptions : class
        {
            return services
                .AddAlgoType<TAlgo>(typeName)
                .ConfigureOptions<AlgoOptionsConfigurator<TOptions>>();
        }
    }
}