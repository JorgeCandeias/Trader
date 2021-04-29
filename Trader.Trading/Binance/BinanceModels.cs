namespace Trader.Trading.Binance
{
    internal record ErrorModel(int Code, string Msg);

    internal record ServerTimeModel(long ServerTime);

    internal record RateLimiterModel(
        string RateLimitType,
        string Interval,
        int IntervalNum,
        int Limit);

    internal record SymbolFilterModel(
        string FilterType,
        decimal MinPrice,
        decimal MaxPrice,
        decimal TickSize,
        decimal MultiplierUp,
        decimal MultiplierDown,
        decimal MinQty,
        decimal MaxQty,
        decimal StepSize,
        decimal MinNotional,
        bool ApplyToMarket,
        int AvgPriceMins,
        int Limit,
        int MaxNumOrders,
        int MaxNumAlgoOrders,
        int MaxNumIcebergOrders,
        decimal MaxPosition);

    internal record SymbolModel(
        string Symbol,
        string Status,
        string BaseAsset,
        int BaseAssetPrecision,
        string QuoteAsset,
        int QuoteAssetPrecision,
        int BaseCommissionPrecision,
        int QuoteCommissionPrecision,
        string[] OrderTypes,
        bool IcebergAllowed,
        bool OcoAllowed,
        bool QuoteOrderQtyMarketAllowed,
        bool IsSpotTradingAllowed,
        bool IsMarginTradingAllowed,
        SymbolFilterModel[] Filters,
        string[] Permissions);

    internal record ExchangeFilterModel(
        string FilterType,
        int? MaxNumOrders,
        int? MaxNumAlgoOrders);

    internal record ExchangeInfoModel(
        string Timezone,
        long ServerTime,
        RateLimiterModel[] RateLimits,
        ExchangeFilterModel[] ExchangeFilters,
        SymbolModel[] Symbols);

    internal record OrderBookModel(
        int LastUpdateId,
        decimal[][] Bids,
        decimal[][] Asks);

    internal record TradeModel(
        int Id,
        decimal Price,
        decimal Qty,
        decimal QuoteQty,
        long Time,
        bool IsBuyerMaker,
        bool IsBestMatch);

    internal record AvgPriceModel(
        int Mins,
        decimal Price);

    internal record TickerModel(
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
        long OpenTime,
        long CloseTime,
        int FirstId,
        int LastId,
        int Count);

    internal record SymbolPriceTickerModel(
        string Symbol,
        decimal Price);

    internal record SymbolOrderBookTickerModel(
        string Symbol,
        decimal BidPrice,
        decimal BidQty,
        decimal AskPrice,
        decimal AskQty);

    internal record NewOrderRequestModel(
        string Symbol,
        string Side,
        string Type,
        string TimeInForce,
        decimal? Quantity,
        decimal? QuoteOrderQty,
        decimal? Price,
        string NewClientOrderId,
        decimal? StopPrice,
        decimal? IcebergQty,
        string NewOrderRespType,
        long? RecvWindow,
        long Timestamp);

    internal record NewOrderResponseModel(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        long TransactTime,
        decimal Price,
        decimal OrigQty,
        decimal ExecutedQty,
        decimal CummulativeQuoteQty,
        string Status,
        string TimeInForce,
        string Type,
        string Side,
        NewOrderResponseFillModel[] Fills);

    internal record NewOrderResponseFillModel(
        decimal Price,
        decimal Qty,
        decimal Commission,
        string CommissionAsset);

    internal record GetOrderRequestModel(
        string Symbol,
        long? OrderId,
        string OrigClientOrderId,
        long? RecvWindow,
        long? Timestamp);

    internal record GetAllOrdersRequestModel(
        string Symbol,
        long? OrderId,
        long? StartTime,
        long? EndTime,
        int? Limit,
        long? RecvWindow,
        long Timestamp);

    internal record GetOrderResponseModel(
        string Symbol,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OrigQty,
        decimal ExecutedQty,
        decimal CummulativeQuoteQty,
        string Status,
        string TimeInForce,
        string Type,
        string Side,
        decimal StopPrice,
        decimal IcebergQty,
        long Time,
        long UpdateTime,
        bool IsWorking,
        decimal OrigQuoteOrderQty);

    internal record CancelOrderRequestModel(
        string Symbol,
        long? OrderId,
        string OrigClientOrderId,
        string NewClientOrderId,
        long? RecvWindow,
        long Timestamp);

    internal record CancelOrderResponseModel(
        string Symbol,
        string OrigClientOrderId,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OrigQty,
        decimal ExecutedQty,
        decimal CummulativeQuoteQty,
        string Status,
        string TimeInForce,
        string Type,
        string Side);

    internal record CancelAllOrdersRequestModel(
        string Symbol,
        long? RecvWindow,
        long Timestamp);

    internal record CancelAllOrdersResponseModel(

        // shared properties
        string Symbol,
        long OrderListId,

        // standard order properties
        string OrigClientOrderId,
        long OrderId,
        string ClientOrderId,
        decimal Price,
        decimal OrigQty,
        decimal ExecutedQty,
        decimal CummulativeQuoteQty,
        string Status,
        string TimeInForce,
        string Type,
        string Side,

        // oco order properties
        string ContingencyType,
        string ListStatusType,
        string ListOrderStatus,
        string ListClientOrderId,
        long TransactionTime,
        CancelAllOrdersOrderResponseModel[] Orders,
        CancellAllOrdersOrderReportResponseModel[] OrderReports);

    internal record CancelAllOrdersOrderResponseModel(
        string Symbol,
        long OrderId,
        string ClientOrderId);

    internal record CancellAllOrdersOrderReportResponseModel(
        string Symbol,
        string OrigClientOrderId,
        long OrderId,
        long OrderListId,
        string ClientOrderId,
        decimal Price,
        decimal OrigQty,
        decimal ExecutedQty,
        decimal CummulativeQuoteQty,
        string Status,
        string TimeInForce,
        string Type,
        string Side,
        decimal StopPrice,
        decimal IcebergQty);

    internal record GetOpenOrdersRequestModel(
        string Symbol,
        long? RecvWindow,
        long Timestamp);

    internal record AccountRequestModel(
        long? RecvWindow,
        long Timestamp);

    internal record AccountResponseModel(
        decimal MakerCommission,
        decimal TakerCommission,
        decimal BuyerCommission,
        decimal SellerCommission,
        bool CanTrade,
        bool CanWithdraw,
        bool CanDeposit,
        long UpdateTime,
        string AccountType,
        AccountBalanceResponseModel[] Balances,
        string[] Permissions);

    internal record AccountBalanceResponseModel(
        string Asset,
        decimal Free,
        decimal Locked);

    internal record AccountTradesRequestModel(
        string Symbol,
        long? StartTime,
        long? EndTime,
        long? FromId,
        int? Limit,
        long? RecvWindow,
        long Timestamp);

    internal record AccountTradesResponseModel(
        string Symbol,
        long Id,
        long OrderId,
        long OrderListId,
        decimal Price,
        decimal Qty,
        decimal QuoteQty,
        decimal Commission,
        string CommissionAsset,
        long Time,
        bool IsBuyer,
        bool IsMaker,
        bool IsBestMatch);

    internal record ListenKeyResponseModel(
        string ListenKey);

    internal record ListenKeyRequestModel(
        string ListenKey);
}