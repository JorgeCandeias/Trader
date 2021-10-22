using AutoMapper;
using Outcompute.Trader.Data.Sql.Models;
using Outcompute.Trader.Models;
using System.Collections.Generic;

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

            CreateMap<OrderQueryResult, OrderTableParameterEntity>()
                .ForCtorParam(nameof(OrderTableParameterEntity.SymbolId), x => x.MapFrom((source, context) => ((IDictionary<string, int>)context.Items[nameof(OrderTableParameterEntity.SymbolId)])[source.Symbol]));

            CreateMap<CancelStandardOrderResult, CancelOrderEntity>()
                .ForCtorParam(nameof(CancelOrderEntity.SymbolId), x => x.MapFrom((source, context) => context.Items[nameof(CancelOrderEntity.SymbolId)]))
                .ForCtorParam(nameof(CancelOrderEntity.ClientOrderId), x => x.MapFrom(y => y.OriginalClientOrderId));

            CreateMap<Balance, BalanceTableParameterEntity>();

            CreateMap<Balance, BalanceEntity>()
                .ReverseMap();

            CreateMap<MiniTicker, TickerTableParameterEntity>()
                .ForCtorParam(nameof(TickerTableParameterEntity.SymbolId), x => x.MapFrom((source, context) => ((IDictionary<string, int>)context.Items[nameof(TickerTableParameterEntity.SymbolId)])[source.Symbol]));

            CreateMap<MiniTicker, TickerEntity>()
                .ReverseMap();

            CreateMap<Kline, KlineTableParameterEntity>()
                .ForCtorParam(nameof(KlineTableParameterEntity.SymbolId), x => x.MapFrom((source, context) => ((IDictionary<string, int>)context.Items[nameof(KlineTableParameterEntity.SymbolId)])[source.Symbol]));

            CreateMap<Kline, KlineEntity>()
                .ForCtorParam(nameof(KlineEntity.SymbolId), x => x.MapFrom((source, context) => context.Items[nameof(KlineEntity.SymbolId)]));
        }
    }
}