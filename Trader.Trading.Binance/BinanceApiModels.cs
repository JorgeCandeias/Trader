namespace Outcompute.Trader.Trading.Binance;

internal record ApiError(int Code, string Msg);

internal record ApiServerTime(long ServerTime);

internal record ApiRateLimiter(
    string RateLimitType,
    string Interval,
    int IntervalNum,
    int Limit);

internal record ApiSymbolFilter(
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
    int MinTrailingAboveDelta,
    int MaxTrailingAboveDelta,
    int MinTrailingBelowDelta,
    int MaxTrailingBelowDelta,
    decimal MaxPosition)
{
    public static ApiSymbolFilter Empty { get; } = new ApiSymbolFilter(string.Empty, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

internal record ApiSymbol(
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
    ApiSymbolFilter[] Filters,
    string[] Permissions);

internal record ApiExchangeFilter(
    string FilterType,
    int? MaxNumOrders,
    int? MaxNumAlgoOrders);

internal record ApiExchangeInfo(
    string Timezone,
    long ServerTime,
    ApiRateLimiter[] RateLimits,
    ApiExchangeFilter[] ExchangeFilters,
    ApiSymbol[] Symbols);

internal record ApiOrderBook(
    int LastUpdateId,
    decimal[][] Bids,
    decimal[][] Asks);

internal record ApiTrade(
    int Id,
    decimal Price,
    decimal Qty,
    decimal QuoteQty,
    long Time,
    bool IsBuyerMaker,
    bool IsBestMatch);

internal record ApiAvgPrice(
    int Mins,
    decimal Price);

internal record ApiTicker(
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

internal record ApiSymbolPriceTicker(
    string Symbol,
    decimal Price);

internal record ApiSymbolOrderBookTicker(
    string Symbol,
    decimal BidPrice,
    decimal BidQty,
    decimal AskPrice,
    decimal AskQty);

internal record CreateOrderRequest(
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

internal record CreateOrderResponse(
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
    CreateOrderResponseFill[] Fills);

internal record CreateOrderResponseFill(
    decimal Price,
    decimal Qty,
    decimal Commission,
    string CommissionAsset);

internal record GetOrderRequest(
    string Symbol,
    long? OrderId,
    string OrigClientOrderId,
    long? RecvWindow,
    long? Timestamp);

internal record GetAllOrdersRequest(
    string Symbol,
    long? OrderId,
    long? StartTime,
    long? EndTime,
    int? Limit,
    long? RecvWindow,
    long Timestamp);

internal record GetOrderResponse(
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

internal record CancelOrderRequest(
    string Symbol,
    long? OrderId,
    string OrigClientOrderId,
    string NewClientOrderId,
    long? RecvWindow,
    long Timestamp);

internal record CancelOrderResponse(
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

internal record CancelAllOrdersRequest(
    string Symbol,
    long? RecvWindow,
    long Timestamp);

internal record CancelAllOrdersResponse(

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
    CancelAllOrdersResponseOrder[] Orders,
    CancelAllOrdersResponseOrderReport[] OrderReports);

internal record CancelAllOrdersResponseOrder(
    string Symbol,
    long OrderId,
    string ClientOrderId);

internal record CancelAllOrdersResponseOrderReport(
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

internal record GetOpenOrdersRequest(
    string Symbol,
    long? RecvWindow,
    long Timestamp);

internal record GetAccountInfoRequest(
    long? RecvWindow,
    long Timestamp);

internal record GetAccountInfoResponse(
    decimal MakerCommission,
    decimal TakerCommission,
    decimal BuyerCommission,
    decimal SellerCommission,
    bool CanTrade,
    bool CanWithdraw,
    bool CanDeposit,
    long UpdateTime,
    string AccountType,
    GetAccountInfoResponseBalance[] Balances,
    string[] Permissions);

internal record GetAccountInfoResponseBalance(
    string Asset,
    decimal Free,
    decimal Locked);

internal record GetAccountTradesRequest(
    string Symbol,
    long? StartTime,
    long? EndTime,
    long? FromId,
    int? Limit,
    long? RecvWindow,
    long Timestamp);

internal record GetAccountTradesResponse(
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

internal record CreateUserDataStreamResponse(
    string ListenKey);

internal record PingUserDataStreamRequest(
    string ListenKey);

internal record CloseUserDataStreamRequest(
    string ListenKey);

internal record GetKlinesRequest(
    string Symbol,
    string Interval,
    long StartTime,
    long EndTime,
    int Limit);

internal record GetKlinesResponse(
    long OpenTime,
    long CloseTime,
    string OpenPrice,
    string HighPrice,
    string LowPrice,
    string ClosePrice,
    string Volume,
    string QuoteAssetVolume,
    int TradeCount,
    string TakerBuyBaseAssetVolume,
    string TakerBuyQuoteAssetVolume);

internal record GetFlexibleProductPositionsRequest(
    string Asset,
    long? RecvWindow,
    long Timestamp);

internal record GetFlexibleProductPositionsResponse(
    decimal AnnualInterestRate,
    string Asset,
    decimal AvgAnnualInterestRate,
    bool CanRedeem,
    decimal DailyInterestRate,
    decimal FreeAmount,
    decimal FreezeAmount,
    decimal LockedAmount,
    string ProductId,
    string ProductName,
    decimal RedeemingAmount,
    decimal TodayPurchasedAmount,
    decimal TotalAmount,
    decimal TotalInterest);

internal record GetLeftDailyRedemptionQuotaOnFlexibleProductRequest(
    string ProductId,
    string Type,
    long? RecvWindow,
    long Timestamp);

internal record GetLeftDailyRedemptionQuotaOnFlexibleProductResponse(
    string Asset,
    decimal DailyQuota,
    decimal LeftQuota,
    decimal MinRedemptionAmount);

internal record RedeemFlexibleProductRequest(
    string ProductId,
    decimal Amount,
    string Type,
    long? RecvWindow,
    long Timestamp);

internal record GetFlexibleProductListRequest(
    string Status,
    string Featured,
    long? Current,
    long? Size,
    long? RecvWindow,
    long Timestamp);

internal record GetFlexibleProductListResponse(
    string Asset,
    decimal AvgAnnualInterestRate,
    bool CanPurchase,
    bool CanRedeem,
    decimal DailyInterestPerThousand,
    bool Featured,
    decimal MinPurchaseAmount,
    string ProductId,
    decimal PurchasedAmount,
    string Status,
    decimal UpLimit,
    decimal UpLimitPerUser);

internal record GetSwapPoolsResponse(
    long PoolId,
    string PoolName,
    string[] Assets);

internal record GetSwapPoolLiquidityRequest(
    long? PoolId,
    long? RecvWindow,
    long Timestamp);

internal record GetSwapPoolLiquidityResponse(
    long PoolId,
    string PoolName,
    long UpdateTime,
    Dictionary<string, decimal> Liquidity,
    GetSwapPoolLiquidityResponseShare Share);

internal record GetSwapPoolLiquidityResponseShare(
    decimal ShareAmount,
    decimal SharePercentage,
    Dictionary<string, decimal> Asset);

internal record AddSwapPoolLiquidityRequest(
    long PoolId,
    string? Type,
    string Asset,
    decimal Quantity,
    long? RecvWindow,
    long Timestamp);

internal record AddSwapPoolLiquidityResponse(
    long OperationId);

internal record RemoveSwapPoolLiquidityRequest(
    long PoolId,
    string Type,
    string? Asset,
    decimal ShareAmount,
    long? RecvWindow,
    long Timestamp);

internal record RemoveSwapPoolLiquidityResponse(
    long OperationId);

internal record GetSwapPoolConfigurationRequest(
    long? PoolId,
    long? RecvWindow,
    long Timestamp);

internal record GetSwapPoolConfigurationResponse(
    long PoolId,
    string PoolName,
    long UpdateTime,
    GetSwapPoolConfigurationResponseLiquidity Liquidity,
    Dictionary<string, GetSwapPoolConfigurationResponseAssetConfigure> AssetConfigure);

internal record GetSwapPoolConfigurationResponseLiquidity(
    decimal MinRedeemShare,
    decimal SlippageTolerance);

internal record GetSwapPoolConfigurationResponseAssetConfigure(
    decimal MinAdd,
    decimal MaxAdd,
    decimal MinSwap,
    decimal MaxSwap);

internal record AddSwapPoolLiquidityPreviewRequest(
    long PoolId,
    string Type,
    string QuoteAsset,
    decimal QuoteQty,
    long? RecvWindow,
    long Timestamp);

internal record AddSwapPoolLiquidityPreviewResponse(
    string QuoteAsset,
    string BaseAsset,
    decimal QuoteAmt,
    decimal BaseAmt,
    decimal Price,
    decimal Share,
    decimal Slippage,
    decimal Fee);

internal record GetSwapPoolQuoteRequest(
    string QuoteAsset,
    string BaseAsset,
    decimal QuoteQty,
    long? RecvWindow,
    long Timestamp);

internal record GetSwapPoolQuoteResponse(
    string QuoteAsset,
    string BaseAsset,
    decimal QuoteQty,
    decimal BaseQty,
    decimal Price,
    decimal Slippage,
    decimal Fee);

internal record MarketDataStreamMessage(
    ExternalError? Error,
    MiniTicker? MiniTicker,
    Kline? Kline,
    MarketDataStreamResult? Result)
{
    public static MarketDataStreamMessage Empty { get; } = new MarketDataStreamMessage(null, null, null, null);
}

internal record MarketDataStreamRequest(
    string Method,
    string[] Params,
    long Id);

internal record MarketDataStreamResult(
    string Result,
    long Id);