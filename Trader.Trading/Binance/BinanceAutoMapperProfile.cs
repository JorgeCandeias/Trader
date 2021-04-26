using AutoMapper;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using Trader.Models;
using Trader.Trading.Binance.Converters;

namespace Trader.Trading.Binance
{
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
            CreateMap<string, TimeZoneInfo>().ConvertUsing<TimeZoneInfoConverter>();
            CreateMap<long, TimeSpan>().ConvertUsing<TimeSpanConverter>();
            CreateMap<decimal[], Bid>().ConvertUsing(x => new Bid(x[0], x[1]));
            CreateMap<decimal[], Ask>().ConvertUsing(x => new Ask(x[0], x[1]));

            // model to base type mappings
            CreateMap<TimeSpan, long>().ConvertUsing<TimeSpanConverter>();
            CreateMap<ServerTimeModel, DateTime>().ConvertUsing<ServerTimeConverter>();
            CreateMap<KlineInterval, string>().ConvertUsing<KlineIntervalConverter>();
            CreateMap<OrderType, string>().ConvertUsing<OrderTypeConverter>();
            CreateMap<OrderSide, string>().ConvertUsing<OrderSideConverter>();
            CreateMap<TimeInForce, string>().ConvertUsing<TimeInForceConverter>();
            CreateMap<NewOrderResponseType, string>().ConvertUsing<NewOrderResponseTypeConverter>();
            CreateMap<ContingencyType, string>().ConvertUsing<ContingencyTypeConverter>();
            CreateMap<OcoStatus, string>().ConvertUsing<OcoStatusConverter>();
            CreateMap<OcoOrderStatus, string>().ConvertUsing<OcoOrderStatusConverter>();
            CreateMap<AccountType, string>().ConvertUsing<AccountTypeConverter>();

            // simple model mappings
            CreateMap<ExchangeInfoModel, ExchangeInfo>();
            CreateMap<AvgPriceModel, AvgPrice>();
            CreateMap<TickerModel, Ticker>();
            CreateMap<SymbolPriceTickerModel, SymbolPriceTicker>();

            // complex model mappings
            CreateMap<RateLimiterModel, RateLimit>().ConvertUsing<RateLimitConverter>();
            CreateMap<ExchangeFilterModel, ExchangeFilter>().ConvertUsing<ExchangeFilterConverter>();
            CreateMap<SymbolFilterModel, SymbolFilter>().ConvertUsing<SymbolFilterConverter>();
            CreateMap<OrderBookModel, OrderBook>().ConvertUsing<OrderBookConverter>();
            CreateMap<CancelAllOrdersResponseModel, CancelOrderResultBase>().ConvertUsing<CancelAllOrdersResponseModelConverter>();

            // renaming model mappings
            CreateMap<SymbolModel, Symbol>()
                .ForCtorParam(nameof(Symbol.Name), x => x.MapFrom(y => y.Symbol))
                .ForCtorParam(nameof(Symbol.IsIcebergAllowed), x => x.MapFrom(y => y.IcebergAllowed))
                .ForCtorParam(nameof(Symbol.IsOcoAllowed), x => x.MapFrom(y => y.OcoAllowed))
                .ForCtorParam(nameof(Symbol.IsQuoteOrderQuantityMarketAllowed), x => x.MapFrom(y => y.QuoteOrderQtyMarketAllowed));

            CreateMap<TradeModel, Trade>()
                .ForCtorParam(nameof(Trade.Quantity), x => x.MapFrom(y => y.Qty))
                .ForCtorParam(nameof(Trade.QuoteQuantity), x => x.MapFrom(y => y.QuoteQty));

            CreateMap<IDictionary<string, JsonElement>, AggTrade>()
                .ForCtorParam(nameof(AggTrade.AggregateTradeId), x => x.MapFrom(y => y["a"].GetInt32()))
                .ForCtorParam(nameof(AggTrade.Price), x => x.MapFrom(y => y["p"].GetString()))
                .ForCtorParam(nameof(AggTrade.Quantity), x => x.MapFrom(y => y["q"].GetString()))
                .ForCtorParam(nameof(AggTrade.FirstTradeId), x => x.MapFrom(y => y["f"].GetInt32()))
                .ForCtorParam(nameof(AggTrade.LastTradeId), x => x.MapFrom(y => y["l"].GetInt32()))
                .ForCtorParam(nameof(AggTrade.Timestamp), x => x.MapFrom(y => y["T"].GetInt64()))
                .ForCtorParam(nameof(AggTrade.IsBuyerMaker), x => x.MapFrom(y => y["m"].GetBoolean()))
                .ForCtorParam(nameof(AggTrade.IsBestMatch), x => x.MapFrom(y => y["M"].GetBoolean()));

            CreateMap<JsonElement[], Kline>()
                .ForCtorParam(nameof(Kline.OpenTime), x => x.MapFrom(y => y[0].GetInt64()))
                .ForCtorParam(nameof(Kline.Open), x => x.MapFrom(y => y[1].GetString()))
                .ForCtorParam(nameof(Kline.High), x => x.MapFrom(y => y[2].GetString()))
                .ForCtorParam(nameof(Kline.Low), x => x.MapFrom(y => y[3].GetString()))
                .ForCtorParam(nameof(Kline.Close), x => x.MapFrom(y => y[4].GetString()))
                .ForCtorParam(nameof(Kline.Volume), x => x.MapFrom(y => y[5].GetString()))
                .ForCtorParam(nameof(Kline.CloseTime), x => x.MapFrom(y => y[6].GetInt64()))
                .ForCtorParam(nameof(Kline.QuoteAssetVolume), x => x.MapFrom(y => y[7].GetString()))
                .ForCtorParam(nameof(Kline.NumberOfTrades), x => x.MapFrom(y => y[8].GetInt32()))
                .ForCtorParam(nameof(Kline.TakerBuyBaseAssetVolume), x => x.MapFrom(y => y[9].GetString()))
                .ForCtorParam(nameof(Kline.TakerBuyQuoteAssetVolume), x => x.MapFrom(y => y[10].GetString()))
                .ForCtorParam(nameof(Kline.Ignore), x => x.MapFrom(y => y[11].GetString()));

            CreateMap<SymbolOrderBookTickerModel, SymbolOrderBookTicker>()
                .ForCtorParam(nameof(SymbolOrderBookTicker.BidQuantity), x => x.MapFrom(y => y.BidQty))
                .ForCtorParam(nameof(SymbolOrderBookTicker.AskQuantity), x => x.MapFrom(y => y.AskQty));

            CreateMap<Order, NewOrderRequestModel>()
                .ForCtorParam(nameof(NewOrderRequestModel.QuoteOrderQty), x => x.MapFrom(y => y.QuoteOrderQuantity))
                .ForCtorParam(nameof(NewOrderRequestModel.IcebergQty), x => x.MapFrom(y => y.IcebergQuantity))
                .ForCtorParam(nameof(NewOrderRequestModel.NewOrderRespType), x => x.MapFrom(y => y.NewOrderResponseType))
                .ForCtorParam(nameof(NewOrderRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<NewOrderResponseModel, OrderResult>()
                .ForCtorParam(nameof(OrderResult.TransactionTime), x => x.MapFrom(y => y.TransactTime))
                .ForCtorParam(nameof(OrderResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(OrderResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(OrderResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty));

            CreateMap<NewOrderResponseFillModel, OrderFill>()
                .ForCtorParam(nameof(OrderFill.Quantity), x => x.MapFrom(y => y.Qty));

            CreateMap<OrderQuery, GetOrderRequestModel>()
                .ForCtorParam(nameof(GetOrderRequestModel.OrigClientOrderId), x => x.MapFrom(y => y.OriginalClientOrderId))
                .ForCtorParam(nameof(GetOrderRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetOrderResponseModel, OrderQueryResult>()
                .ForCtorParam(nameof(OrderQueryResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(OrderQueryResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(OrderQueryResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty))
                .ForCtorParam(nameof(OrderQueryResult.IcebergQuantity), x => x.MapFrom(y => y.IcebergQty))
                .ForCtorParam(nameof(OrderQueryResult.OriginalQuoteOrderQuantity), x => x.MapFrom(y => y.OrigQuoteOrderQty));

            CreateMap<CancelStandardOrder, CancelOrderRequestModel>()
                .ForCtorParam(nameof(CancelOrderRequestModel.OrigClientOrderId), x => x.MapFrom(y => y.OriginalClientOrderId))
                .ForCtorParam(nameof(CancelOrderRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<CancelOrderResponseModel, CancelStandardOrderResult>()
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalClientOrderId), x => x.MapFrom(y => y.OrigClientOrderId))
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty));

            CreateMap<CancelAllOrders, CancelAllOrdersRequestModel>()
                .ForCtorParam(nameof(CancelAllOrdersRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<CancelAllOrdersResponseModel, CancelStandardOrderResult>()
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalClientOrderId), x => x.MapFrom(y => y.OrigClientOrderId))
                .ForCtorParam(nameof(CancelStandardOrderResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(CancelStandardOrderResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty));

            CreateMap<CancelAllOrdersResponseModel, CancelOcoOrderResult>();

            CreateMap<CancellAllOrdersOrderReportResponseModel, CancelOcoOrderOrderReportResult>()
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.OriginalClientOrderId), x => x.MapFrom(y => y.OrigClientOrderId))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.OriginalQuantity), x => x.MapFrom(y => y.OrigQty))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.ExecutedQuantity), x => x.MapFrom(y => y.ExecutedQty))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.CummulativeQuoteQuantity), x => x.MapFrom(y => y.CummulativeQuoteQty))
                .ForCtorParam(nameof(CancelOcoOrderOrderReportResult.IcebergQuantity), x => x.MapFrom(y => y.IcebergQty));

            CreateMap<CancelAllOrdersOrderResponseModel, CancelOcoOrderOrderResult>();

            CreateMap<GetOpenOrders, GetOpenOrdersRequestModel>()
                .ForCtorParam(nameof(GetOpenOrdersRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<GetAccountInfo, AccountRequestModel>()
                .ForCtorParam(nameof(AccountRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<AccountResponseModel, AccountInfo>();

            CreateMap<AccountBalanceResponseModel, AccountBalance>();

            CreateMap<GetAccountTrades, AccountTradesRequestModel>()
                .ForCtorParam(nameof(AccountTradesRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            CreateMap<AccountTradesResponseModel, AccountTrade>()
                .ForCtorParam(nameof(AccountTrade.Quantity), x => x.MapFrom(y => y.Qty))
                .ForCtorParam(nameof(AccountTrade.QuoteQuantity), x => x.MapFrom(y => y.QuoteQty));

            CreateMap<GetAllOrders, GetAllOrdersRequestModel>()
                .ForCtorParam(nameof(GetAllOrdersRequestModel.RecvWindow), x => x.MapFrom(y => y.ReceiveWindow));

            // open converters
            CreateMap(typeof(IEnumerable<>), typeof(ImmutableList<>)).ConvertUsing(typeof(ImmutableListConverter<,>));
        }
    }
}