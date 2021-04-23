using System;

namespace Trader.Data.Sql
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

    internal record TradeEntity(
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
        bool IsBestMatch);
}