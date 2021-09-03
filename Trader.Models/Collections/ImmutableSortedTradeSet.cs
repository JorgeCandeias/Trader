using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models.Collections
{
    public sealed class ImmutableSortedTradeSet : IImmutableSet<AccountTrade>
    {
        private readonly ImmutableSortedSet<AccountTrade> _set;

        internal ImmutableSortedTradeSet(ImmutableSortedSet<AccountTrade> set)
        {
            if (set is null) throw new ArgumentNullException(nameof(set));
            if (set.KeyComparer is not Comparer) throw new ArgumentOutOfRangeException(nameof(set));

            _set = set;
        }

        #region Set

        public AccountTrade this[int index] => _set[index];

        public IComparer<AccountTrade> KeyComparer => _set.KeyComparer;

        public AccountTrade? Max => _set.Max;

        public AccountTrade? Min => _set.Min;

        public bool IsEmpty => _set.IsEmpty;

        public int Count => _set.Count;

        public ImmutableSortedTradeSet Add(AccountTrade value) => _set.Contains(value) ? this : new(_set.Add(value));

        public ImmutableSortedTradeSet Clear() => _set.IsEmpty ? this : new(_set.Clear());

        public ImmutableSortedTradeSet Except(IEnumerable<AccountTrade> other) => new(_set.Except(other));

        public ImmutableSortedTradeSet Intersect(IEnumerable<AccountTrade> other) => new(_set.Intersect(other));

        public ImmutableSortedTradeSet Remove(AccountTrade value) => _set.Contains(value) ? new(_set.Remove(value)) : this;

        public ImmutableSortedTradeSet SymmetricExcept(IEnumerable<AccountTrade> other) => new(_set.SymmetricExcept(other));

        public ImmutableSortedTradeSet Union(IEnumerable<AccountTrade> other) => new(_set.Union(other));

        public IEnumerator<AccountTrade> GetEnumerator() => _set.GetEnumerator();

        public bool Contains(AccountTrade value) => _set.Contains(value);

        public bool IsProperSubsetOf(IEnumerable<AccountTrade> other) => _set.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<AccountTrade> other) => _set.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<AccountTrade> other) => _set.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<AccountTrade> other) => _set.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<AccountTrade> other) => _set.Overlaps(other);

        public bool SetEquals(IEnumerable<AccountTrade> other) => _set.SetEquals(other);

        public bool TryGetValue(AccountTrade equalValue, out AccountTrade actualValue) => _set.TryGetValue(equalValue, out actualValue);

        #endregion Set

        #region IImmutableSet

        IImmutableSet<AccountTrade> IImmutableSet<AccountTrade>.Add(AccountTrade value) => Add(value);

        IImmutableSet<AccountTrade> IImmutableSet<AccountTrade>.Clear() => Clear();

        IImmutableSet<AccountTrade> IImmutableSet<AccountTrade>.Except(IEnumerable<AccountTrade> other) => Except(other);

        IImmutableSet<AccountTrade> IImmutableSet<AccountTrade>.Intersect(IEnumerable<AccountTrade> other) => Intersect(other);

        IImmutableSet<AccountTrade> IImmutableSet<AccountTrade>.Remove(AccountTrade value) => Remove(value);

        IImmutableSet<AccountTrade> IImmutableSet<AccountTrade>.SymmetricExcept(IEnumerable<AccountTrade> other) => SymmetricExcept(other);

        IImmutableSet<AccountTrade> IImmutableSet<AccountTrade>.Union(IEnumerable<AccountTrade> other) => Union(other);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IImmutableSet

        #region Builder

        public static Builder CreateBuilder() => new();

        public sealed class Builder : ISet<AccountTrade>
        {
            private readonly ImmutableSortedSet<AccountTrade>.Builder _builder = ImmutableSortedSet.CreateBuilder(Comparer.Default);

            #region Set

            public int Count => _builder.Count;

            public bool IsReadOnly => false;

            public bool Add(AccountTrade item) => _builder.Add(item);

            public void Clear() => _builder.Clear();

            public bool Contains(AccountTrade item) => _builder.Contains(item);

            public void CopyTo(AccountTrade[] array, int arrayIndex) => ((ICollection<AccountTrade>)_builder).CopyTo(array, arrayIndex);

            public void ExceptWith(IEnumerable<AccountTrade> other) => _builder.ExceptWith(other);

            public IEnumerator<AccountTrade> GetEnumerator() => _builder.GetEnumerator();

            public void IntersectWith(IEnumerable<AccountTrade> other) => _builder.IntersectWith(other);

            public bool IsProperSubsetOf(IEnumerable<AccountTrade> other) => _builder.IsProperSubsetOf(other);

            public bool IsProperSupersetOf(IEnumerable<AccountTrade> other) => _builder.IsProperSupersetOf(other);

            public bool IsSubsetOf(IEnumerable<AccountTrade> other) => _builder.IsSubsetOf(other);

            public bool IsSupersetOf(IEnumerable<AccountTrade> other) => _builder.IsSupersetOf(other);

            public bool Overlaps(IEnumerable<AccountTrade> other) => _builder.Overlaps(other);

            public bool Remove(AccountTrade item) => _builder.Remove(item);

            public bool SetEquals(IEnumerable<AccountTrade> other) => _builder.SetEquals(other);

            public void SymmetricExceptWith(IEnumerable<AccountTrade> other) => _builder.SymmetricExceptWith(other);

            public void UnionWith(IEnumerable<AccountTrade> other) => _builder.UnionWith(other);

            void ICollection<AccountTrade>.Add(AccountTrade item) => ((ICollection<AccountTrade>)_builder).Add(item);

            IEnumerator IEnumerable.GetEnumerator() => _builder.GetEnumerator();

            #endregion Set

            public ImmutableSortedTradeSet ToImmutable() => new(_builder.ToImmutable());
        }

        #endregion Builder

        #region Comparer

        internal class Comparer : IComparer<AccountTrade>
        {
            private Comparer()
            {
            }

            public int Compare(AccountTrade? x, AccountTrade? y)
            {
                if (x is null) throw new ArgumentNullException(nameof(x));
                if (y is null) throw new ArgumentNullException(nameof(y));

                var bySymbol = Comparer<string>.Default.Compare(x.Symbol, y.Symbol);
                if (bySymbol is not 0) return bySymbol;

                return Comparer<long>.Default.Compare(x.Id, y.Id);
            }

            public static Comparer Default { get; } = new Comparer();
        }

        #endregion Comparer

        #region Helpers

        public static readonly ImmutableSortedTradeSet Empty = new(ImmutableSortedSet.Create<AccountTrade>(Comparer.Default));

        public static ImmutableSortedTradeSet Create(IEnumerable<AccountTrade> items)
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