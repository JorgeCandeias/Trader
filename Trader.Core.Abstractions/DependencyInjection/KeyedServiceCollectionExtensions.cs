using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class KeyedServiceCollectionExtensions
    {
        public static TService? GetKeyedService<TKey, TService>(this IServiceProvider services, TKey key)
            where TKey : IEquatable<TKey>
            where TService : class
        {
            // finds the last service registered with the specified key
            // note that we are deliberatly avoiding linq here to keep this path fast

            IKeyedService<TKey, TService>? last = null;

            foreach (var service in services.GetServices<IKeyedService<TKey, TService>>())
            {
                if (EqualityComparer<TKey>.Default.Equals(service.Key, key))
                {
                    last = service;
                }
            }

            return last?.GetService();
        }

        public static TService GetRequiredKeyedService<TKey, TService>(this IServiceProvider services, TKey key)
            where TKey : IEquatable<TKey>
            where TService : class
        {
            return services.GetKeyedService<TKey, TService>(key) ?? throw new KeyNotFoundException(key.ToString());
        }

        public static IEnumerable<TService> GetKeyedServices<TKey, TService>(this IServiceProvider services)
            where TKey : IEquatable<TKey>
            where TService : class
        {
            foreach (var service in services.GetServices<IKeyedService<TKey, TService>>())
            {
                var instance = service.GetService();

                if (instance is not null) yield return instance;
            }
        }
    }
}