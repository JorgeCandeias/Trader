using System;
using System.Collections.Immutable;

namespace Trader.Models
{
    public record UserDataStreamMessage();

    public record OutboundAccountPositionUserDataStreamMessage(
        DateTime EventTime,
        DateTime LastAccountUpdateTime,
        ImmutableList<OutboundAccountPositionBalanceUserDataStreamMessage> Balances)
        : UserDataStreamMessage;

    public record OutboundAccountPositionBalanceUserDataStreamMessage(
        string Asset,
        decimal Free,
        decimal Locked);

    public record BalanceUpdateUserDataStreamMessage(
        DateTime EventTime,
        string Asset,
        decimal BalanceDelta,
        DateTime ClearTime)
        : UserDataStreamMessage;

    public record ExecutionReportUserDataStreamMessage(
        DateTime EventTime,
        string Symbol,
        string ClientOrderId,
        OrderSide OrderSide,
        OrderType OrderType,
        TimeInForce TimeInForce,
        decimal OrderQuantity,
        decimal OrderPrice,
        decimal StopPrice,
        decimal IcebergQuantity,
        long OrderListId,
        string OriginalClientOrderId,
        ExecutionType ExecutionType,
        OrderStatus OrderStatus,
        string OrderRejectReason,
        long OrderId,
        decimal LastExecutedQuantity,
        decimal CummulativeFilledQuantity,
        decimal LastExecutedPrice,
        decimal CommissionAmount,
        string CommissionAsset,
        DateTime TransactionTime,
        long TradeId,
        bool IsBookOrder,
        bool IsMakerOrder,
        DateTime OrderCreatedTime,
        decimal CummulativeQuoteAssetTransactedQuantity,
        decimal LastQuoteAssetTransactedQuantity,
        decimal QuoteOrderQuantity)
        : UserDataStreamMessage;

    public record ListStatusUserDataStreamMessage(
        DateTime EventTime,
        string Symbol,
        long OrderListId,
        ContingencyType ContingencyType,
        OcoStatus ListStatusType,
        OcoOrderStatus ListOrderStatus,
        string ListRejectReason,
        string ListClientOrderId,
        DateTime TransactionTime,
        ImmutableList<ListStatusItemUserDataStreamMessage> Items)
        : UserDataStreamMessage;

    public record ListStatusItemUserDataStreamMessage(
        string Symbol,
        long OrderId,
        string ClientOrderId);
}