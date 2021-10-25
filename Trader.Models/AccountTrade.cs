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
        public static AccountTrade Empty { get; } = new AccountTrade(string.Empty, 0, 0, 0, 0, 0, 0, 0, string.Empty, DateTime.MinValue, false, false, false);

        public static IComparer<AccountTrade> KeyComparer { get; } = new KeyComparerInternal();

        public static IEqualityComparer<AccountTrade> TradeKeyEqualityComparer { get; } = new TradeKeyEqualityComparerInternal();

        private sealed class KeyComparerInternal : Comparer<AccountTrade>
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
                        var bySymbol = Comparer<string>.Default.Compare(x.Symbol, y.Symbol);
                        if (bySymbol != 0) return bySymbol;

                        return Comparer<long>.Default.Compare(x.Id, y.Id);
                    }
                }
            }
        }

        private sealed class TradeKeyEqualityComparerInternal : EqualityComparer<AccountTrade>
        {
            public override bool Equals(AccountTrade? x, AccountTrade? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                else
                {
                    return
                        y is not null &&
                        EqualityComparer<string>.Default.Equals(x.Symbol, y.Symbol) &&
                        EqualityComparer<long>.Default.Equals(x.Id, y.Id);
                }
            }

            public override int GetHashCode([DisallowNull] AccountTrade obj)
            {
                return HashCode.Combine(obj.Symbol, obj.Id);
            }
        }
    }
}