using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class KeyedSingletonService<TKey, TService> : IKeyedService<TKey, TService>
        where TKey : IEquatable<TKey>
        where TService : class
    {
        private readonly Lazy<TService?> _instance;

        public KeyedSingletonService(TKey key, IServiceProvider services, Func<IServiceProvider, TKey, TService?> factory)
        {
            Key = key;
            _instance = new Lazy<TService?>(() => factory(services, key), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public TKey Key { get; }

        public bool Equals(TKey? other) => EqualityComparer<TKey>.Default.Equals(Key, other);

        public TService? GetService() => _instance.Value;
    }

    internal class KeyedSingletonService<TKey, TService, TInstance> : KeyedSingletonService<TKey, TService>
        where TKey : IEquatable<TKey>
        where TService : class
        where TInstance : TService
    {
        private static readonly ObjectFactory _factory = ActivatorUtilities.CreateFactory(typeof(TInstance), Array.Empty<Type>());

        public KeyedSingletonService(TKey key, IServiceProvider services)
            : base(key, services, (sp, k) => (TInstance)_factory(sp, null))
        {
        }
    }
}