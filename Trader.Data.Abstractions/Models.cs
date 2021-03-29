using System;
using System.Collections.Immutable;

namespace Trader.Data
{
    public enum RateLimitType
    {
        None,
        RequestWeight,
        Orders,
        RawRequests
    }

    public enum SymbolStatus
    {
        None,
        PreTrading,
        Trading,
        PostTrading,
        EndOfDay,
        Halt,
        AuctionMatch,
        Break
    }

    public enum OrderType
    {
        None = 0,
        Limit = 1,
        LimitMaker = 2,
        Market = 3,
        StopLoss = 4,
        StopLossLimit = 5,
        TakeProfit = 6,
        TakeProfitLimit = 7
    }

    public enum Permission
    {
        None,
        Spot,
        Margin,
        Leveraged
    }

    public enum KlineInterval
    {
        None,
        Minutes1,
        Minutes3,
        Minutes5,
        Minutes15,
        Minutes30,
        Hours1,
        Hours2,
        Hours4,
        Hours6,
        Hours8,
        Hours12,
        Days1,
        Days3,
        Weeks1,
        Months1
    }

    public enum OrderSide
    {
        None = 0,
        Buy = 1,
        Sell = 2
    }

    public enum TimeInForce
    {
        None = 0,
        GoodTillCanceled = 1,
        ImmediateOrCancel = 2,
        FillOrKill = 3
    }

    public enum NewOrderResponseType
    {
        None,
        Acknowledge,
        Result,
        Full
    }

    public enum OrderStatus
    {
        None = 0,
        New = 1,
        PartiallyFilled = 2,
        Filled = 3,
        Canceled = 4,
        PendingCancel = 5,
        Rejected = 6,
        Expired = 7
    }

    public static class OrderStatusExtensions
    {
        public static bool IsCompletedStatus(this OrderStatus status)
        {
            return
                status == OrderStatus.Filled ||
                status == OrderStatus.Canceled ||
                status == OrderStatus.Rejected ||
                status == OrderStatus.Expired;
        }

        public static bool IsTransientStatus(this OrderStatus status)
        {
            return
                status == OrderStatus.New ||
                status == OrderStatus.PartiallyFilled ||
                status == OrderStatus.PendingCancel;
        }
    }

    public enum ContingencyType
    {
        None,
        Oco
    }

    public enum OcoStatus
    {
        None,
        Response,
        ExecutionStarted,
        AllDone
    }

    public enum OcoOrderStatus
    {
        None,
        Executing,
        AllDone,
        Reject
    }

    public enum AccountType
    {
        None,
        Spot
    }

    public record RateLimit(
        RateLimitType Type,
        TimeSpan TimeSpan,
        int Limit);

    public record ExchangeFilter;

    public record ExchangeMaxNumberOfOrdersFilter(int Value) : ExchangeFilter;

    public record ExchangeMaxNumberOfAlgoOrdersFilter(int Value) : ExchangeFilter;

    public record SymbolFilter;
    public record PriceSymbolFilter(decimal MinPrice, decimal MaxPrice, decimal TickSize) : SymbolFilter;
    public record PercentPriceSymbolFilter(decimal MultiplierUp, decimal MultiplierDown, int AvgPriceMins) : SymbolFilter;
    public record LotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter;
    public record MinNotionalSymbolFilter(decimal MinNotional, bool ApplyToMarket, int AvgPriceMins) : SymbolFilter;
    public record IcebergPartsSymbolFilter(int Limit) : SymbolFilter;
    public record MarketLotSizeSymbolFilter(decimal MinQuantity, decimal MaxQuantity, decimal StepSize) : SymbolFilter;
    public record MaxNumberOfOrdersSymbolFilter(int MaxNumberOfOrders) : SymbolFilter;
    public record MaxNumberOfAlgoOrdersSymbolFilter(int MaxNumberOfAlgoOrders) : SymbolFilter;
    public record MaxNumberOfIcebergOrdersSymbolFilter(int MaxNumberOfIcebergOrders) : SymbolFilter;
    public record MaxPositionSymbolFilter(decimal MaxPosition) : SymbolFilter;

    public record Symbol(
        string Name,
        SymbolStatus Status,
        string BaseAsset,
        int BaseAssetPrecision,
        string QuoteAsset,
        int QuoteAssetPrecision,
        int BaseCommissionPrecision,
        int QuoteCommissionPrecision,
        ImmutableList<OrderType> OrderTypes,
        bool IsIcebergAllowed,
        bool IsOcoAllowed,
        bool IsQuoteOrderQuantityMarketAllowed,
        bool IsSpotTradingAllowed,
        bool IsMarginTradingAllowed,
        ImmutableList<SymbolFilter> Filters,
        ImmutableList<Permission> Permissions);

    public record ExchangeInfo(
        TimeZoneInfo TimeZone,
        DateTime ServerTime,
        ImmutableList<RateLimit> RateLimits,
        ImmutableList<ExchangeFilter> ExchangeFilters,
        ImmutableList<Symbol> Symbols);

    public record Bid(
        decimal Price,
        decimal Quantity);

    public record Ask(
        decimal Price,
        decimal Quantity);

    public record OrderBook(
        int LastUpdateId,
        ImmutableList<Bid> Bids,
        ImmutableList<Ask> Asks);

    public record Trade(
        int Id,
        decimal Price,
        decimal Quantity,
        decimal QuoteQuantity,
        DateTime Time,
        bool IsBuyerMaker,
        bool IsBestMatch);

    public record AggTrade(
        int AggregateTradeId,
        decimal Price,
        decimal Quantity,
        int FirstTradeId,
        int LastTradeId,
        DateTime Timestamp,
        bool IsBuyerMaker,
        bool IsBestMatch);

    public record Kline(
        DateTime OpenTime,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal Volume,
        DateTime CloseTime,
        decimal QuoteAssetVolume,
        int NumberOfTrades,
        decimal TakerBuyBaseAssetVolume,
        decimal TakerBuyQuoteAssetVolume,
        decimal Ignore);

    public record AvgPrice(
        int Mins,
        decimal Price);

    public record Ticker(
        string Symbol,
        decimal PriceChange,
        decimal PriceChangePercent,
        decimal WeightedAvgPrice,
        decimal PrevClosePrice,
        decimal LastPrice,
        decimal LastQty,
        decimal BidPrice,
        decimal AskPrice,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal Volume,
        decimal QuoteVolume,
        DateTime OpenTime,
        DateTime CloseTime,
        int FirstId,
        int LastId,
        int Count);

    public record SymbolPriceTicker(
        string Symbol,
        decimal Price);

    public record SymbolOrderBookTicker(
        string Symbol,
        decimal BidPrice,
        decimal BidQuantity,
        decimal AskPrice,
        decimal AskQuantity);

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

    public record OrderQuery(
        string Symbol,
        long? OrderId,
        string? OriginalClientOrderId,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

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

    public record CancelStandardOrder(
        string Symbol,
        long? OrderId,
        string? OriginalClientOrderId,
        string? NewClientOrderId,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    public record CancelOrderResultBase();

    public record CancelStandardOrderResult(
        string Symbol,
        string OriginalClientOrderId,
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
        OrderSide Side) : CancelOrderResultBase;

    public record CancelOcoOrderResult(
        long OrderListId,
        ContingencyType ContingencyType,
        OcoStatus ListStatusType,
        OcoOrderStatus ListOrderStatus,
        string ListClientOrderId,
        DateTime TransactionTime,
        string Symbol,
        ImmutableList<CancelOcoOrderOrderResult> Orders,
        ImmutableList<CancelOcoOrderOrderReportResult> OrderReports) : CancelOrderResultBase;

    public record CancelOcoOrderOrderReportResult(
        string Symbol,
        string OriginalClientOrderId,
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
        decimal IcebergQuantity);

    public record CancelOcoOrderOrderResult(
        string Symbol,
        long OrderId,
        string ClientOrderId);

    public record CancelAllOrders(
        string Symbol,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    public record GetOpenOrders(
        string Symbol,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    public record GetAllOrders(
        string Symbol,
        long? OrderId,
        DateTime? StartTime,
        DateTime? EndTime,
        int? Limit,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    public record GetAccountInfo(
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    public record AccountInfo(
        decimal MakerCommission,
        decimal TakerCommission,
        decimal BuyerCommission,
        decimal SellerCommission,
        bool CanTrade,
        bool CanWithdraw,
        bool CanDeposit,
        DateTime UpdateTime,
        AccountType AccountType,
        ImmutableList<AccountBalance> Balances,
        ImmutableList<Permission> Permissions);

    public record AccountBalance(
        string Asset,
        decimal Free,
        decimal Locked);

    public record GetAccountTrades(
        string Symbol,
        DateTime? StartTime,
        DateTime? EndTime,
        long? FromId,
        int? Limit,
        TimeSpan? ReceiveWindow,
        DateTime Timestamp);

    public record AccountTrade(
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

    public record Usage(
        RateLimitType Type,
        TimeSpan Window,
        int Count);
}