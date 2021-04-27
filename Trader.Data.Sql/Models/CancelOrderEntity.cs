namespace Trader.Data.Sql.Models
{
    internal record CancelOrderEntity(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OriginalQuantity,
        decimal ExecutedQuantity,
        decimal CummulativeQuoteQuantity,
        int Status,
        int TimeInForce,
        int Type,
        int Side);
}