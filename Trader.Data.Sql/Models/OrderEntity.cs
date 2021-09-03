using System;

namespace Outcompute.Trader.Data.Sql.Models
{
    internal record OrderEntity(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OriginalQuantity,
        decimal ExecutedQuantity,
        decimal CummulativeQuoteQuantity,
        decimal OriginalQuoteOrderQuantity,
        int Status,
        int TimeInForce,
        int Type,
        int Side,
        decimal StopPrice,
        decimal IcebergQuantity,
        DateTime Time,
        DateTime UpdateTime,
        bool IsWorking);
}