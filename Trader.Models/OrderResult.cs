using System;
using System.Collections.Immutable;

namespace Outcompute.Trader.Models
{
    public record OrderResult(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        DateTime TransactionTime,
        decimal Price,
        decimal OriginalQuantity,
        decimal ExecutedQuantity,
        decimal CummulativeQuoteQuantity,
        OrderStatus Status,
        TimeInForce TimeInForce,
        OrderType Type,
        OrderSide Side,
        ImmutableList<OrderFill> Fills)
    {
        public static OrderResult Empty { get; } = new OrderResult(
            string.Empty,
            0,
            0,
            string.Empty,
            DateTime.MinValue,
            0,
            0,
            0,
            0,
            OrderStatus.None,
            TimeInForce.None,
            OrderType.None,
            OrderSide.None,
            ImmutableList<OrderFill>.Empty);
    }

    public record OrderFill(
        decimal Price,
        decimal Quantity,
        decimal Commission,
        string CommissionAsset);
}