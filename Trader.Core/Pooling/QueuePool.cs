using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Core.Pooling
{
    public static class QueuePool<T>
    {
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Type Rooting Pattern")]
        public static ObjectPool<Queue<T>> Shared { get; } = CorePoolProvider.Default.Create(QueuePooledObjectPolicy.Default);

        private sealed class QueuePooledObjectPolicy : IPooledObjectPolicy<Queue<T>>
        {
            public Queue<T> Create()
            {
                return new Queue<T>();
            }

            public bool Return(Queue<T> obj)
            {
                obj.Clear();
                return true;
            }

            public static QueuePooledObjectPolicy Default { get; } = new QueuePooledObjectPolicy();
        }
    }
}