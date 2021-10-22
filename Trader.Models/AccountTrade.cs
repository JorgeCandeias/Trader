using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record AccountTrade(
        string Symbol,
        long Id,
        long OrderId,
        long OrderListId,
        decimal Price,
        decimal Quantity,
        decimal QuoteQuantity,
        decimal Commission,
        string CommissionAsset,
        DateTime Time,
        bool IsBuyer,
        bool IsMaker,
        bool IsBestMatch)
    {
        public static IComparer<AccountTrade> TradeIdComparer { get; } = new TradeIdComparerInternal();

        public static IEqualityComparer<AccountTrade> TradeIdEqualityComparer { get; } = new TradeIdEqualityComparerInternal();

        private sealed class TradeIdComparerInternal : Comparer<AccountTrade>
        {
            public override int Compare(AccountTrade? x, AccountTrade? y)
            {
                if (x is null)
                {
                    if (y is null)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    if (y is null)
                    {
                        return -1;
                    }
                    else
                    {
                        return Comparer<long>.Default.Compare(x.Id, y.Id);
                    }
                }
            }
        }

        private sealed class TradeIdEqualityComparerInternal : EqualityComparer<AccountTrade>
        {
            public override bool Equals(AccountTrade? x, AccountTrade? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                else
                {
                    return y is not null && y.Id == x.Id;
                }
            }

            public override int GetHashCode([DisallowNull] AccountTrade obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}