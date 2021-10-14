using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoType<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            return services.AddSingletonNamedService<IAlgoFactory, AlgoFactory<TAlgo>>(typeName);
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