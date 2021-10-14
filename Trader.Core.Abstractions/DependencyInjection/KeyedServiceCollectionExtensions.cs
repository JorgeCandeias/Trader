using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class KeyedServiceCollectionExtensions
    {
        #region Keyed

        [SuppressMessage("Major Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "N/A")]
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

        #endregion Keyed

        #region Named

        public static TService? GetNamedService<TService>(this IServiceProvider services, string name)
            where TService : class
        {
            return GetKeyedService<string, TService>(services, name);
        }

        public static TService GetRequiredNamedService<TService>(this IServiceProvider services, string name)
            where TService : class
        {
            return GetRequiredKeyedService<string, TService>(services, name);
        }

        public static IEnumerable<TService> GetNamedServices<TService>(this IServiceProvider services)
            where TService : class
        {
            foreach (var service in services.GetServices<IKeyedService<string, TService>>())
            {
                var instance = service.GetService();

                if (instance is not null) yield return instance;
            }
        }

        #endregion Named
    }
}