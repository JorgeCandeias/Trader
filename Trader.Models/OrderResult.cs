using System;
using System.Collections.Immutable;

namespace Trader.Models
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
        ImmutableList<OrderFill> Fills);

    public record OrderFill(
        decimal Price,
        decimal Quantity,
        decimal Commission,
        string CommissionAsset);
}