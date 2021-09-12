using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class KeyedService<TKey, TService> : IKeyedService<TKey, TService>
        where TKey : IEquatable<TKey>
        where TService : class
    {
        private readonly IServiceProvider _services;
        public readonly Func<IServiceProvider, TKey, TService?> _factory;

        public KeyedService(TKey key, IServiceProvider services, Func<IServiceProvider, TKey, TService?> factory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public TKey Key { get; }

        public TService? GetService()
        {
            return _factory(_services, Key);
        }

        public bool Equals(TKey? other)
        {
            return EqualityComparer<TKey>.Default.Equals(Key, other);
        }
    }

    internal class KeyedService<TKey, TService, TInstance> : KeyedService<TKey, TService>
        where TKey : IEquatable<TKey>
        where TService : class
        where TInstance : TService
    {
        // todo: fix this
        public KeyedService(TKey key, IServiceProvider services)
            : base(key, services, (sp, k) => sp.GetService<TInstance>())
        {
        }
    }
}