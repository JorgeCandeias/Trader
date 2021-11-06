using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoType<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            services.TryAddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));

            return services.AddTransientNamedService<IAlgoFactory, AlgoFactory<TAlgo>>(typeName);
        }

        public static IServiceCollection AddAlgoType<TAlgo, TOptions>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
            where TOptions : class
        {
            return services
                .AddAlgoType<TAlgo>(typeName)
                .ConfigureOptions<AlgoUserOptionsConfigurator<TOptions>>();
        }
    }
}