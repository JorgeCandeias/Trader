using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Indicators.ObjectPools
{
    internal static class QueuePool<T>
    {
        public static ObjectPool<Queue<T>> Shared { get; } = IndicatorPoolProvider.Default.Create(QueuePooledObjectPolicy.Default);

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