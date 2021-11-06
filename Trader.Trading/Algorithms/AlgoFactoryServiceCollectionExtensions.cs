using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgoType<TAlgo, TOptions>(this IServiceCollection services)
            where TAlgo : IAlgo
            where TOptions : class
        {
            return services
                .AddAlgoType<TAlgo>()
                .AddAlgoOptionsType<TOptions>();
        }

        public static IServiceCollection AddAlgoType<TAlgo, TOptions>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
            where TOptions : class
        {
            return services
                .AddAlgoType<TAlgo>(typeName)
                .AddAlgoOptionsType<TOptions>();
        }

        public static IServiceCollection AddAlgoType<TAlgo>(this IServiceCollection services)
            where TAlgo : IAlgo
        {
            var typeName = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            return services.AddAlgoType<TAlgo>(typeName);
        }

        public static IServiceCollection AddAlgoType<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            return services
                .AddAlgoTypeEntry<TAlgo>(typeName)
                .AddTransientNamedService<IAlgoFactory, AlgoFactory<TAlgo>>(typeName);
        }

        public static IServiceCollection AddAlgoOptionsType<TOptions>(this IServiceCollection services)
            where TOptions : class
        {
            return services.ConfigureOptions<AlgoUserOptionsConfigurator<TOptions>>();
        }

        public static IServiceCollection TryAddKeyedServiceCollection(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));

            return services;
        }
    }
}