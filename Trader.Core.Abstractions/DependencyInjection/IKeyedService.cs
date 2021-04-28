using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IKeyedService<TKey, out TService>
        where TKey : IEquatable<TKey>
        where TService : class
    {
        TKey Key { get; }

        TService? GetService();
    }
}