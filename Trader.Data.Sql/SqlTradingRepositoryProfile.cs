using AutoMapper;
using Outcompute.Trader.Data.Sql.Models;
using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Linq;

namespace Outcompute.Trader.Data.Sql
{
    internal class SqlTradingRepositoryProfile : Profile
    {
        public SqlTradingRepositoryProfile()
        {
            CreateMap<OrderQueryResult, OrderEntity>()
                .ReverseMap();

            CreateMap<AccountTrade, TradeEntity>()
                .ReverseMap();

            CreateMap<AccountTrade, TradeTableParameterEntity>()
                .ForCtorParam(nameof(TradeTableParameterEntity.SymbolId), x => x.MapFrom((source, context) => ((IDictionary<string, int>)context.Items[nameof(TradeTableParameterEntity.SymbolId)])[source.Symbol]));

            CreateMap<OrderResult, OrderQueryResult>()
                .ForCtorParam(nameof(OrderQueryResult.StopPrice), x => x.MapFrom((source, context) => context.Items[nameof(OrderQueryResult.StopPrice)]))
                .ForCtorParam(nameof(OrderQueryResult.IcebergQuantity), x => x.MapFrom((source, context) => context.Items[nameof(OrderQueryResult.IcebergQuantity)]))
                .ForCtorParam(nameof(OrderQueryResult.Time), x => x.MapFrom(y => y.TransactionTime))
                .ForCtorParam(nameof(OrderQueryResult.UpdateTime), x => x.MapFrom(y => y.TransactionTime))
                .ForCtorParam(nameof(OrderQueryResult.IsWorking), x => x.MapFrom(_ => true))
                .ForCtorParam(nameof(OrderQueryResult.OriginalQuoteOrderQuantity), x => x.MapFrom((source, context) => context.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)]));

            CreateMap<OrderQueryResult, OrderTableParameterEntity>()
                .ForCtorParam(nameof(OrderTableParameterEntity.SymbolId), x => x.MapFrom((source, context) => ((IDictionary<string, int>)context.Items[nameof(OrderTableParameterEntity.SymbolId)])[source.Symbol]));

            CreateMap<CancelStandardOrderResult, CancelOrderEntity>()
                .ForCtorParam(nameof(CancelOrderEntity.SymbolId), x => x.MapFrom((source, context) => context.Items[nameof(CancelOrderEntity.SymbolId)]))
                .ForCtorParam(nameof(CancelOrderEntity.ClientOrderId), x => x.MapFrom(y => y.OriginalClientOrderId));

            CreateMap<AccountInfo, IEnumerable<Balance>>()
                .ConvertUsing(x => x.Balances.Select(y => new Balance(
                    y.Asset,
                    y.Free,
                    y.Locked,
                    x.UpdateTime)));

            CreateMap<Balance, BalanceTableParameterEntity>();

            CreateMap<Balance, BalanceEntity>()
                .ReverseMap();

            CreateMap<MiniTicker, TickerTableParameterEntity>()
                .ForCtorParam(nameof(TickerTableParameterEntity.SymbolId), x => x.MapFrom((source, context) => ((IDictionary<string, int>)context.Items[nameof(TickerTableParameterEntity.SymbolId)])[source.Symbol]));

            CreateMap<TickerEntity, MiniTicker>();

            CreateMap<Kline, KlineTableParameterEntity>()
                .ForCtorParam(nameof(KlineTableParameterEntity.SymbolId), x => x.MapFrom((source, context) => ((IDictionary<string, int>)context.Items[nameof(KlineTableParameterEntity.SymbolId)])[source.Symbol]));
        }
    }
}