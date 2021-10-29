using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal class BandEqualityComparer : IEqualityComparer<Band>
    {
        public static BandEqualityComparer Default { get; } = new BandEqualityComparer();

        public bool Equals(Band? x, Band? y)
        {
            if (x is null)
            {
                return y is null;
            }
            else
            {
                return y is not null && EqualsCore(x, y);
            }
        }

        private static bool EqualsCore(Band x, Band y)
        {
            return EqualityComparer<decimal>.Default.Equals(x.OpenPrice, y.OpenPrice)
                && EqualityComparer<long>.Default.Equals(x.OpenOrderId, y.OpenOrderId)
                && EqualityComparer<Guid>.Default.Equals(x.Id, y.Id);
        }

        public int GetHashCode([DisallowNull] Band obj)
        {
            return HashCode.Combine(obj.OpenPrice, obj.OpenOrderId, obj.Id);
        }
    }
}