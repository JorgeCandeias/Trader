using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class KeyedServiceCollectionExtensions
    {
        #region Keyed

        public static IServiceCollection AddKeyedTransient<TKey, TService>(this IServiceCollection services, TKey key, Func<IServiceProvider, TKey, TService> factory)
            where TKey : IEquatable<TKey>
            where TService : class
        {
            return services.AddSingleton<IKeyedService<TKey, TService>>(sp => new KeyedService<TKey, TService>(key, sp, factory));
        }

        public static IServiceCollection AddKeyedTransient<TKey, TService, TInstance>(this IServiceCollection services, TKey key)
            where TKey : IEquatable<TKey>
            where TService : class
            where TInstance : TService
        {
            return services.AddSingleton<IKeyedService<TKey, TService>>(sp => new KeyedService<TKey, TService, TInstance>(key, sp));
        }

        public static IServiceCollection AddKeyedSingleton<TKey, TService>(this IServiceCollection services, TKey key, Func<IServiceProvider, TKey, TService> factory)
            where TKey : IEquatable<TKey>
            where TService : class
        {
            return services.AddSingleton<IKeyedService<TKey, TService>>(sp => new KeyedSingletonService<TKey, TService>(key, sp, factory));
        }

        public static IServiceCollection AddKeyedSingleton<TKey, TService, TInstance>(this IServiceCollection services, TKey key)
            where TKey : IEquatable<TKey>
            where TService : class
            where TInstance : TService
        {
            return services.AddSingleton<IKeyedService<TKey, TService>>(sp => new KeyedSingletonService<TKey, TService, TInstance>(key, sp));
        }

        #endregion Keyed

        #region Named

        public static IServiceCollection AddNamedTransient<TService>(this IServiceCollection services, string name, Func<IServiceProvider, string, TService> factory)
            where TService : class
        {
            return services.AddSingleton<IKeyedService<string, TService>>(sp => new KeyedService<string, TService>(name, sp, factory));
        }

        public static IServiceCollection AddNamedTransient<TService, TInstance>(this IServiceCollection services, string name)
            where TService : class
            where TInstance : TService
        {
            return services.AddSingleton<IKeyedService<string, TService>>(sp => new KeyedService<string, TService, TInstance>(name, sp));
        }

        public static IServiceCollection AddNamedSingleton<TService>(this IServiceCollection services, string name, Func<IServiceProvider, string, TService> factory)
            where TService : class
        {
            return services.AddSingleton<IKeyedService<string, TService>>(sp => new KeyedSingletonService<string, TService>(name, sp, factory));
        }

        public static IServiceCollection AddNamedSingleton<TService, TInstance>(this IServiceCollection services, string name)
            where TService : class
            where TInstance : TService
        {
            return services.AddSingleton<IKeyedService<string, TService>>(sp => new KeyedSingletonService<string, TService, TInstance>(name, sp));
        }

        #endregion Named
    }
}