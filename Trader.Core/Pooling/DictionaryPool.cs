using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Core.Pooling
{
    public static class DictionaryPool<TKey, TValue>
        where TKey : notnull
    {
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Type Rooting Pattern")]
        public static ObjectPool<Dictionary<TKey, TValue>> Shared { get; } = CorePoolProvider.Default.Create(DictionaryPooledObjectPolicy.Default);

        private sealed class DictionaryPooledObjectPolicy : IPooledObjectPolicy<Dictionary<TKey, TValue>>
        {
            public Dictionary<TKey, TValue> Create()
            {
                return new Dictionary<TKey, TValue>();
            }

            public bool Return(Dictionary<TKey, TValue> obj)
            {
                obj.Clear();
                return true;
            }

            public static DictionaryPooledObjectPolicy Default { get; } = new DictionaryPooledObjectPolicy();
        }
    }
}