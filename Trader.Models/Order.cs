using System;

namespace Trader.Models
{
    public record Order(
        string Symbol,
        OrderSide Side,
        OrderType Type,
        TimeInForce? TimeInForce,
        decimal? Quantity,
        decimal? QuoteOrderQuantity,
        decimal? Price,
        string? NewClientOrderId,
        decimal? StopPrice,
        decimal? IcebergQuantity,
        NewOrderResponseType NewOrderResponseType,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);
}