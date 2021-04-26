using AutoMapper;

namespace Trader.Data.Sql
{
    internal class SqlTraderRepositoryProfile : Profile
    {
        public SqlTraderRepositoryProfile()
        {
            CreateMap<OrderQueryResult, OrderEntity>()
                .ReverseMap();

            CreateMap<AccountTrade, TradeEntity>()
                .ReverseMap();

            // todo: this is a destructive conversion - ask the missing parameters from the context instead so the algos can supply them as needed
            CreateMap<OrderResult, OrderQueryResult>()
                .ForCtorParam(nameof(OrderQueryResult.StopPrice), x => x.MapFrom(_ => 0m))
                .ForCtorParam(nameof(OrderQueryResult.IcebergQuantity), x => x.MapFrom(_ => 0m))
                .ForCtorParam(nameof(OrderQueryResult.Time), x => x.MapFrom(y => y.TransactionTime))
                .ForCtorParam(nameof(OrderQueryResult.UpdateTime), x => x.MapFrom(y => y.TransactionTime))
                .ForCtorParam(nameof(OrderQueryResult.IsWorking), x => x.MapFrom(_ => true))
                .ForCtorParam(nameof(OrderQueryResult.OriginalQuoteOrderQuantity), x => x.MapFrom(_ => 0m));

            CreateMap<OrderQueryResult, OrderTableParameterEntity>();

            CreateMap<CancelStandardOrderResult, CancelOrderEntity>();
        }
    }
}