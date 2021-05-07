using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Buffers
{
    public static class ArrayPoolExtensions
    {
        public static IArraySegmentOwner<T> RentSegment<T>(this ArrayPool<T> pool, int count)
        {
            _ = pool ?? throw new ArgumentNullException(nameof(pool));

            var buffer = pool.Rent(count);
            var owner = Typed<T>.ArrayOwnerPool.Get();
            owner.Assign(pool, buffer, 0, count, Typed<T>.ArrayOwnerPool);
            return owner;
        }

        public static IArraySegmentOwner<T> RentSegmentWith<T>(this ArrayPool<T> pool, IReadOnlyCollection<T> items)
        {
            return RentSegmentWith(pool, items, x => x);
        }

        public static IArraySegmentOwner<TValue> RentSegmentWith<TItem, TValue>(this ArrayPool<TValue> pool, IReadOnlyCollection<TItem> items, Func<TItem, TValue> selector)
        {
            _ = pool ?? throw new ArgumentNullException(nameof(pool));
            _ = items ?? throw new ArgumentNullException(nameof(items));
            _ = selector ?? throw new ArgumentNullException(nameof(selector));

            var buffer = pool.Rent(items.Count);
            var count = 0;

            foreach (var item in items)
            {
                buffer[count++] = selector(item);
            }

            var owner = Typed<TValue>.ArrayOwnerPool.Get();
            owner.Assign(pool, buffer, 0, items.Count, Typed<TValue>.ArrayOwnerPool);
            return owner;
        }

        private static class Typed<T>
        {
            public static ObjectPool<ArraySegmentOwner<T>> ArrayOwnerPool { get; } = new DefaultObjectPool<ArraySegmentOwner<T>>(new DefaultPooledObjectPolicy<ArraySegmentOwner<T>>());
        }
    }

    public interface IArraySegmentOwner<T> : IDisposable
    {
        ArraySegment<T> Segment { get; }
    }

    internal sealed class ArraySegmentOwner<T> : IArraySegmentOwner<T>
    {
        private ArrayPool<T>? _pool;
        private T[]? _array;
        private ObjectPool<ArraySegmentOwner<T>>? _owners;

        internal void Assign(ArrayPool<T> pool, T[] array, int offset, int count, ObjectPool<ArraySegmentOwner<T>> owners)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _array = array ?? throw new ArgumentNullException(nameof(array));
            _owners = owners ?? throw new ArgumentNullException(nameof(owners));

            Segment = new ArraySegment<T>(_array, offset, count);
        }

        internal void Return()
        {
            // test for double calls
            if (_pool is null) return;
            if (_array is null) return;
            if (_owners is null) return;

            // return the owned array to its pool
            _pool.Return(_array);

            // return self to the owners pool
            _owners.Return(this);

            // make refs available for gc
            _array = null;
            _pool = null;
            _owners = null;
        }

        public ArraySegment<T> Segment { get; private set; }

        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Disposable Ownership Pattern")]
        public void Dispose()
        {
            Return();
        }

        ~ArraySegmentOwner()
        {
            Dispose();
        }
    }
}