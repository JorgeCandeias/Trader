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
        decimal OriginalQuoteOrderQuantity);
}