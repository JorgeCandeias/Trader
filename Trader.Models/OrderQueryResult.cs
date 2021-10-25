using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models
{
    [Immutable]
    public record OrderQueryResult(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OriginalQuantity,
        decimal ExecutedQuantity,
        decimal CummulativeQuoteQuantity,
        OrderStatus Status,
        TimeInForce TimeInForce,
        OrderType Type,
        OrderSide Side,
        decimal StopPrice,
        decimal IcebergQuantity,
        DateTime Time,
        DateTime UpdateTime,
        bool IsWorking,
        decimal OriginalQuoteOrderQuantity)
    {
        public static OrderQueryResult Empty { get; } = new OrderQueryResult(
            string.Empty,
            0,
            0,
            string.Empty,
            0,
            0,
            0,
            0,
            OrderStatus.None,
            TimeInForce.None,
            OrderType.None,
            OrderSide.None,
            0,
            0,
            DateTime.MinValue,
            DateTime.MinValue,
            false,
            0);

        public static IComparer<OrderQueryResult> KeyComparer { get; } = new KeyComparerInternal();

        // todo: replace usage of this this with key comparer
        public static IEqualityComparer<OrderQueryResult> OrderIdEqualityComparer { get; } = new OrderIdEqualityComparerInternal();

        private sealed class KeyComparerInternal : Comparer<OrderQueryResult>
        {
            public override int Compare(OrderQueryResult? x, OrderQueryResult? y)
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

                        return Comparer<long>.Default.Compare(x.OrderId, y.OrderId);
                    }
                }
            }
        }

        private sealed class OrderIdEqualityComparerInternal : IEqualityComparer<OrderQueryResult>
        {
            public bool Equals(OrderQueryResult? x, OrderQueryResult? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                else
                {
                    return y is not null && y.OrderId == x.OrderId;
                }
            }

            public int GetHashCode([DisallowNull] OrderQueryResult obj)
            {
                return obj.OrderId.GetHashCode();
            }
        }
    }
}