using System;

namespace Outcompute.Trader.Models
{
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
    }
}