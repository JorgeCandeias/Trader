using AutoMapper;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Binance.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Outcompute.Trader.Trading.Binance
{
    [ExcludeFromCodeCoverage]
    internal class BinanceAutoMapperProfile : Profile
    {
        public BinanceAutoMapperProfile()
        {
            // base type to base type mappings
            CreateMap<DateTime, long>().ConvertUsing<DateTimeConverter>();
            CreateMap<long, DateTime>().ConvertUsing<DateTimeConverter>();

            // base type to model mappings
            CreateMap<string, SymbolStatus>().ConvertUsing<SymbolStatusConverter>();
            CreateMap<string, OrderType>().ConvertUsing<OrderTypeConverter>();
            CreateMap<string, Permission>().ConvertUsing<PermissionConverter>();
            CreateMap<string, OrderSide>().ConvertUsing<OrderSideConverter>();
            CreateMap<string, TimeInForce>().ConvertUsing<TimeInForceConverter>();
            CreateMap<string, OrderStatus>().ConvertUsing<OrderStatusConverter>();
            CreateMap<string, ContingencyType>().ConvertUsing<ContingencyTypeConverter>();
            CreateMap<string, OcoStatus>().ConvertUsing<OcoStatusConverter>();
            CreateMap<string, OcoOrderStatus>().ConvertUsing<OcoOrderStatusConverter>();
            CreateMap<string, AccountType>().ConvertUsing<AccountTypeConverter>();
            CreateMap<string, ExecutionType>().ConvertUsing<ExecutionTypeConverter>();
            CreateMap<string, TimeZoneInfo>().ConvertUsing<TimeZoneInfoConverter>();
            CreateMap<string, KlineInterval>().ConvertUsing<KlineIntervalConverter>();
            CreateMap<string, SavingsRedemptionType>().ConvertUsing<SavingsRedemptionTypeConverter>();
            CreateMap<string, SavingsStatus>().ConvertUsing<SavingsStatusConverter>();
            CreateMap<string, SavingsFeatured>().ConvertUsing<SavingsFeaturedConverter>();
            CreateMap<string, SwapPoolLiquidityType>().ConvertUsing<SwapPoolLiquidityTypeConverter>();
            CreateMap<long, TimeSpan>().ConvertUsing<TimeSpanConverter>();
            CreateMap<decimal[], Bid>().ConvertUsing(x => new Bid(x[0], x[1]));
            CreateMap<decimal[], Ask>().ConvertUsing(x => new Ask(x[0], x[1]));

            // model to base type mappings
            CreateMap<TimeSpan, long>().ConvertUsing<TimeSpanConverter>();
            CreateMap<ApiServerTime, DateTime>().ConvertUsing<ApiServerTimeConverter>();
            CreateMap<KlineInterval, string>().ConvertUsing<KlineIntervalConverter>();
            CreateMap<OrderType, string>().ConvertUsing<OrderTypeConverter>();
            CreateMap<OrderSide, string>().ConvertUsing<OrderSideConverter>();
            CreateMap<TimeInForce, string>().ConvertUsing<TimeInForceConverter>();
            CreateMap<NewOrderResponseType, string>().ConvertUsing<NewOrderResponseTypeConverter>();
            CreateMap<ContingencyType, string>().ConvertUsing<ContingencyTypeConverter>();
            CreateMap<OcoStatus, string>().ConvertUsing<OcoStatusConverter>();
            CreateMap<OcoOrderStatus, string>().ConvertUsing<OcoOrderStatusConverter>();
            CreateMap<AccountType, string>().ConvertUsing<AccountTypeConverter>();
            CreateMap<ExecutionType, string>().ConvertUsing<ExecutionTypeConverter>();
            CreateMap<SavingsRedemptionType, string>().ConvertUsing<SavingsRedemptionTypeConverter>();
            CreateMap<SavingsStatus, string>().ConvertUsing<SavingsStatusConverter>();
            CreateMap<SavingsFeatured, string>().ConvertUsing<SavingsFeaturedConverter>();
            CreateMap<SwapPoolLiquidityType, string>().ConvertUsing<SwapPoolLiquidityTypeConverter>();

            // simple model mappings
            CreateMap<ApiExchangeInfo, ExchangeInfo>();
            CreateMap<ApiTicker, Ticker>();
            CreateMap<ApiSymbolPriceTicker, SymbolPriceTicker>();

            // complex model mappings
            CreateMap<ApiRateLimiter, RateLimit>().ConvertUsing<ApiRateLimiterConverter>();
            CreateMap<ApiExchangeFilter, ExchangeFilter>().ConvertUsing<ApiExchangeFilterConverter>();
            CreateMap<ApiSymbolFilter, SymbolFilter>().ConvertUsing<ApiSymbolFilterConverter>();
            CreateMap<IEnumerable<ApiSymbolFilter>, SymbolFilters>().ConvertUsing<ApiSymbolFiltersConverter>();
            CreateMap<ApiOrderBook, OrderBook>().ConvertUsing<ApiOrderBookConverter>();
            CreateMap<CancelAllOrdersResponse, CancelOrderResult>().ConvertUsing<CancelAllOrdersResponseConverter>();

            // renaming model mappings
            CreateMap<ApiSymbol, Symbol>()
                .ForCtorParam(nameof(Symbol.Name), x => x.MapFrom(y => y.Symbol))
                .ForCtorParam(nameof(Symbol.IsIcebergAllowed), x => x.MapFrom(y => y.IcebergAllowed))
                .ForCtorParam(nameof(Symbol.IsOcoAllowed), x => x.MapFrom(y => y.OcoAllowed))
                .ForCtorParam(nameof(Symbol.IsQuoteOrderQuantityMarketAllowed), x => x.MapFrom(y => y.QuoteOrderQtyMarketAllowed));

            CreateMap<ApiTrade, Trade>()
                .ForCtorParam(nameof(Trade.Quantity), x => x.MapFrom(y => y.Qty))
                .ForCtorParam(nameof(Trade.QuoteQuantity), x => x.MapFrom(y => y.QuoteQty));

            CreateMap<ApiSymbolOrderBookTicker, SymbolOrderBookTicker>()
                .ForCtorParam(nameof(SymbolOrderBookTicker.BidQuantity), x => x.MapFrom(y => y.BidQty))
                .ForCtorParam(nameof(SymbolOrderBookTicker.AskQuantity), x => x.MapFrom(y => y.AskQty));

            CreateMap<Order, CreateOrderRequest>()
                .ForCtorParam(nameof(CreateOrderRequest.QuoteOrderQty), x => x.MapFrom(y => y.QuoteOrderQuantity))
                .ForCtorParam(nameof(CreateOrderRequest.IcebergQty), x => x.MapFrom(y => y.IcebergQuantity))
                .ForCtorParam(nameof(CreateOrderRequest.NewOrderRespType), x => x.MapFrom(y => y.NewOrderResponseType))
                .ForCtorParam(nameof(CreateOrderRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<CreateOrderResponse, OrderResult>()
                .ForCtorParam(nameof(OrderResult.TransactionTime), x => x.MapFrom(y => y.TransactTime))
                .ForCtorParam(nameof(OrderResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(OrderResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(OrderResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty));

            CreateMap<CreateOrderResponseFill, OrderFill>()
                .ForCtorParam(nameof(OrderFill.Quantity), x => x.MapFrom(y => y.Qty));

            CreateMap<OrderQuery, GetOrderRequest>()
                .ForCtorParam(nameof(GetOrderRequest.OrigClientOrderId), x => x.MapFrom(y => y.OriginalClientOrderId))
                .ForCtorParam(nameof(GetOrderRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetOrderResponse, OrderQueryResult>()
                .ForCtorParam(nameof(OrderQueryResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(OrderQueryResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(OrderQueryResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty))
                .ForCtorParam(nameof(OrderQueryResult.IcebergQuantity), x => x.MapFrom(y => y.IcebergQty))
                .ForCtorParam(nameof(OrderQueryResult.OriginalQuoteOrderQuantity), x => x.MapFrom(y => y.OrigQuoteOrderQty));

            CreateMap<CancelStandardOrder, CancelOrderRequest>()
                .ForCtorParam(nameof(CancelOrderRequest.OrigClientOrderId), x => x.MapFrom(y => y.OriginalClientOrderId))
                .ForCtorParam(nameof(CancelOrderRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<CancelOrderResponse, CancelStandardOrderResult>()
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalClientOrderId), x => x.MapFrom(y => y.OrigClientOrderId))
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty));

            CreateMap<CancelAllOrders, CancelAllOrdersRequest>()
                .ForCtorParam(nameof(CancelAllOrdersRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<CancelAllOrdersResponse, CancelStandardOrderResult>()
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalClientOrderId), x => x.MapFrom(y => y.OrigClientOrderId))
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty));

            CreateMap<CancelAllOrdersResponse, CancelOcoOrderResult>();

            CreateMap<CancelAllOrdersResponseOrderReport, CancelOcoOrderOrderReportResult>()
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.OriginalClientOrderId), x => x.MapFrom(y => y.OrigClientOrderId))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.IcebergQuantity), x => x.MapFrom(y => y.IcebergQty));

            CreateMap<CancelAllOrdersResponseOrder, CancelOcoOrderOrderResult>();

            CreateMap<GetOpenOrders, GetOpenOrdersRequest>()
                .ForCtorParam(nameof(GetOpenOrdersRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetAccountInfo, GetAccountInfoRequest>()
                .ForCtorParam(nameof(GetAccountInfoRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetAccountInfoResponse, AccountInfo>();

            CreateMap<GetAccountInfoResponseBalance, AccountBalance>();

            CreateMap<GetAccountTrades, GetAccountTradesRequest>()
                .ForCtorParam(nameof(GetAccountTradesRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetAccountTradesResponse, AccountTrade>()
                .ForCtorParam(nameof(AccountTrade.Quantity), x => x.MapFrom(y => y.Qty))
                .ForCtorParam(nameof(AccountTrade.QuoteQuantity), x => x.MapFrom(y => y.QuoteQty));

            CreateMap<GetAllOrders, GetAllOrdersRequest>()
                .ForCtorParam(nameof(GetAllOrdersRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetKlines, GetKlinesRequest>();

            CreateMap<JsonElement[], GetKlinesResponse>()
                .ForCtorParam(nameof(GetKlinesResponse.OpenTime), x => x.MapFrom(y => y[0].GetInt64()))
                .ForCtorParam(nameof(GetKlinesResponse.OpenPrice), x => x.MapFrom(y => y[1].GetRequiredDecimalFromString()))
                .ForCtorParam(nameof(GetKlinesResponse.HighPrice), x => x.MapFrom(y => y[2].GetRequiredDecimalFromString()))
                .ForCtorParam(nameof(GetKlinesResponse.LowPrice), x => x.MapFrom(y => y[3].GetRequiredDecimalFromString()))
                .ForCtorParam(nameof(GetKlinesResponse.ClosePrice), x => x.MapFrom(y => y[4].GetRequiredDecimalFromString()))
                .ForCtorParam(nameof(GetKlinesResponse.Volume), x => x.MapFrom(y => y[5].GetRequiredDecimalFromString()))
                .ForCtorParam(nameof(GetKlinesResponse.CloseTime), x => x.MapFrom(y => y[6].GetInt64()))
                .ForCtorParam(nameof(GetKlinesResponse.QuoteAssetVolume), x => x.MapFrom(y => y[7].GetRequiredDecimalFromString()))
                .ForCtorParam(nameof(GetKlinesResponse.TradeCount), x => x.MapFrom(y => y[8].GetInt32()))
                .ForCtorParam(nameof(GetKlinesResponse.TakerBuyBaseAssetVolume), x => x.MapFrom(y => y[9].GetRequiredDecimalFromString()))
                .ForCtorParam(nameof(GetKlinesResponse.TakerBuyQuoteAssetVolume), x => x.MapFrom(y => y[10].GetRequiredDecimalFromString()));

            CreateMap<GetKlinesResponse, Kline>()
                .ForCtorParam(nameof(Kline.Symbol), x => x.MapFrom((source, context) => context.Items[nameof(Kline.Symbol)]))
                .ForCtorParam(nameof(Kline.Interval), x => x.MapFrom((source, context) => context.Items[nameof(Kline.Interval)]))
                .ForCtorParam(nameof(Kline.EventTime), x => x.MapFrom(y => y.OpenTime))
                .ForCtorParam(nameof(Kline.FirstTradeId), x => x.MapFrom(y => -1))
                .ForCtorParam(nameof(Kline.LastTradeId), x => x.MapFrom(y => -1))
                .ForCtorParam(nameof(Kline.IsClosed), x => x.MapFrom(y => true));

            CreateMap<GetFlexibleProductPosition, GetFlexibleProductPositionsRequest>()
                .ForCtorParam(nameof(GetFlexibleProductPositionsRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetFlexibleProductPositionsResponse, SavingsPosition>();

            CreateMap<GetLeftDailyRedemptionQuotaOnFlexibleProduct, GetLeftDailyRedemptionQuotaOnFlexibleProductRequest>()
                .ForCtorParam(nameof(GetLeftDailyRedemptionQuotaOnFlexibleProductRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<LeftDailyRedemptionQuotaOnFlexibleProductResponseModel, SavingsQuota>();

            CreateMap<RedeemFlexibleProduct, FlexibleProductRedemptionRequestModel>()
                .ForCtorParam(nameof(FlexibleProductRedemptionRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetFlexibleProduct, FlexibleProductRequestModel>()
                .ForCtorParam(nameof(FlexibleProductRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<FlexibleProductResponseModel, SavingsProduct>();

            // convert payloads from the user data stream
            CreateMap<Memory<byte>, UserDataStreamMessage>().ConvertUsing<UserDataStreamMessageConverter>();

            // convert payloads from the market data stream
            CreateMap<Memory<byte>, MarketDataStreamMessage>().ConvertUsing<MarketDataStreamMessageConverter>();

            CreateMap<SwapPoolResponseModel, SwapPool>();

            CreateMap<GetSwapPoolLiquidity, SwapPoolLiquidityRequestModel>()
                .ForCtorParam(nameof(SwapPoolLiquidityRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<SwapPoolLiquidityResponseModel, SwapPoolLiquidity>()
                .ForCtorParam(nameof(SwapPoolLiquidity.ShareAmount), x => x.MapFrom(y => y.Share.ShareAmount))
                .ForCtorParam(nameof(SwapPoolLiquidity.SharePercentage), x => x.MapFrom(y => y.Share.SharePercentage))
                .ForCtorParam(nameof(SwapPoolLiquidity.AssetShare), x => x.MapFrom(y => y.Share.Asset));

            CreateMap<AddSwapPoolLiquidity, SwapPoolAddLiquidityRequestModel>()
                .ForCtorParam(nameof(SwapPoolAddLiquidityRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<SwapPoolAddLiquidityResponseModel, SwapPoolOperation>();

            CreateMap<RemoveSwapLiquidity, SwapPoolRemoveLiquidityRequest>()
                .ForCtorParam(nameof(SwapPoolRemoveLiquidityRequest.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));
        }
    }
}