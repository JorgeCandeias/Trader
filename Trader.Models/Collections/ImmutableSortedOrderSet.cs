using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models.Collections
{
    public sealed class ImmutableSortedOrderSet : IImmutableSet<OrderQueryResult>
    {
        private readonly ImmutableSortedSet<OrderQueryResult> _set;

        internal ImmutableSortedOrderSet(ImmutableSortedSet<OrderQueryResult> set)
        {
            if (set is null) throw new ArgumentNullException(nameof(set));
            if (set.KeyComparer is not Comparer) throw new ArgumentOutOfRangeException(nameof(set));

            _set = set;
        }

        #region Set

        public OrderQueryResult this[int index] => _set[index];

        public IComparer<OrderQueryResult> KeyComparer => _set.KeyComparer;

        public OrderQueryResult? Max => _set.Max;

        public OrderQueryResult? Min => _set.Min;

        public bool IsEmpty => _set.IsEmpty;

        public int Count => _set.Count;

        public ImmutableSortedOrderSet Add(OrderQueryResult value) => _set.Contains(value) ? this : new(_set.Add(value));

        public ImmutableSortedOrderSet Clear() => _set.IsEmpty ? this : new(_set.Clear());

        public ImmutableSortedOrderSet Except(IEnumerable<OrderQueryResult> other) => new(_set.Except(other));

        public ImmutableSortedOrderSet Intersect(IEnumerable<OrderQueryResult> other) => new(_set.Intersect(other));

        public ImmutableSortedOrderSet Remove(OrderQueryResult value) => _set.Contains(value) ? new(_set.Remove(value)) : this;

        public ImmutableSortedOrderSet SymmetricExcept(IEnumerable<OrderQueryResult> other) => new(_set.SymmetricExcept(other));

        public ImmutableSortedOrderSet Union(IEnumerable<OrderQueryResult> other) => new(_set.Union(other));

        public IEnumerator<OrderQueryResult> GetEnumerator() => _set.GetEnumerator();

        public bool Contains(OrderQueryResult value) => _set.Contains(value);

        public bool IsProperSubsetOf(IEnumerable<OrderQueryResult> other) => _set.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<OrderQueryResult> other) => _set.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<OrderQueryResult> other) => _set.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<OrderQueryResult> other) => _set.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<OrderQueryResult> other) => _set.Overlaps(other);

        public bool SetEquals(IEnumerable<OrderQueryResult> other) => _set.SetEquals(other);

        public bool TryGetValue(OrderQueryResult equalValue, out OrderQueryResult actualValue) => _set.TryGetValue(equalValue, out actualValue);

        #endregion Set

        #region IImmutableSet

        IImmutableSet<OrderQueryResult> IImmutableSet<OrderQueryResult>.Add(OrderQueryResult value) => Add(value);

        IImmutableSet<OrderQueryResult> IImmutableSet<OrderQueryResult>.Clear() => Clear();

        IImmutableSet<OrderQueryResult> IImmutableSet<OrderQueryResult>.Except(IEnumerable<OrderQueryResult> other) => Except(other);

        IImmutableSet<OrderQueryResult> IImmutableSet<OrderQueryResult>.Intersect(IEnumerable<OrderQueryResult> other) => Intersect(other);

        IImmutableSet<OrderQueryResult> IImmutableSet<OrderQueryResult>.Remove(OrderQueryResult value) => Remove(value);

        IImmutableSet<OrderQueryResult> IImmutableSet<OrderQueryResult>.SymmetricExcept(IEnumerable<OrderQueryResult> other) => SymmetricExcept(other);

        IImmutableSet<OrderQueryResult> IImmutableSet<OrderQueryResult>.Union(IEnumerable<OrderQueryResult> other) => Union(other);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IImmutableSet

        #region Builder

        public static Builder CreateBuilder() => new();

        public sealed class Builder : ISet<OrderQueryResult>
        {
            private readonly ImmutableSortedSet<OrderQueryResult>.Builder _builder = ImmutableSortedSet.CreateBuilder(Comparer.Default);

            #region Set

            public int Count => _builder.Count;

            public bool IsReadOnly => false;

            public bool Add(OrderQueryResult item) => _builder.Add(item);

            public void Clear() => _builder.Clear();

            public bool Contains(OrderQueryResult item) => _builder.Contains(item);

            public void CopyTo(OrderQueryResult[] array, int arrayIndex) => ((ICollection<OrderQueryResult>)_builder).CopyTo(array, arrayIndex);

            public void ExceptWith(IEnumerable<OrderQueryResult> other) => _builder.ExceptWith(other);

            public IEnumerator<OrderQueryResult> GetEnumerator() => _builder.GetEnumerator();

            public void IntersectWith(IEnumerable<OrderQueryResult> other) => _builder.IntersectWith(other);

            public bool IsProperSubsetOf(IEnumerable<OrderQueryResult> other) => _builder.IsProperSubsetOf(other);

            public bool IsProperSupersetOf(IEnumerable<OrderQueryResult> other) => _builder.IsProperSupersetOf(other);

            public bool IsSubsetOf(IEnumerable<OrderQueryResult> other) => _builder.IsSubsetOf(other);

            public bool IsSupersetOf(IEnumerable<OrderQueryResult> other) => _builder.IsSupersetOf(other);

            public bool Overlaps(IEnumerable<OrderQueryResult> other) => _builder.Overlaps(other);

            public bool Remove(OrderQueryResult item) => _builder.Remove(item);

            public bool SetEquals(IEnumerable<OrderQueryResult> other) => _builder.SetEquals(other);

            public void SymmetricExceptWith(IEnumerable<OrderQueryResult> other) => _builder.SymmetricExceptWith(other);

            public void UnionWith(IEnumerable<OrderQueryResult> other) => _builder.UnionWith(other);

            void ICollection<OrderQueryResult>.Add(OrderQueryResult item) => ((ICollection<OrderQueryResult>)_builder).Add(item);

            IEnumerator IEnumerable.GetEnumerator() => _builder.GetEnumerator();

            #endregion Set

            public ImmutableSortedOrderSet ToImmutable() => new(_builder.ToImmutable());
        }

        #endregion Builder

        #region Comparer

        internal class Comparer : IComparer<OrderQueryResult>
        {
            private Comparer()
            {
            }

            public int Compare(OrderQueryResult? x, OrderQueryResult? y)
            {
                if (x is null) throw new ArgumentNullException(nameof(x));
                if (y is null) throw new ArgumentNullException(nameof(y));

                var bySymbol = Comparer<string>.Default.Compare(x.Symbol, y.Symbol);
                if (bySymbol is not 0) return bySymbol;

                return Comparer<long>.Default.Compare(x.OrderId, y.OrderId);
            }

            public static Comparer Default { get; } = new Comparer();
        }

        #endregion Comparer

        #region Helpers

        public static readonly ImmutableSortedOrderSet Empty = new(ImmutableSortedSet.Create<OrderQueryResult>(Comparer.Default));

        public static ImmutableSortedOrderSet Create(IEnumerable<OrderQueryResult> items)
        {
            _ = items ?? throw new ArgumentNullException(nameof(items));

            var builder = new Builder();

            foreach (var item in items)
            {
                builder.Add(item);
            }

            return builder.ToImmutable();
        }

        #endregion Helpers
    }
}