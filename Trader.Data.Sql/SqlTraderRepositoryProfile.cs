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

            CreateMap<AccountTrade, TradeTableParameterEntity>();

            CreateMap<OrderResult, OrderQueryResult>()
                .ForCtorParam(nameof(OrderQueryResult.StopPrice), x => x.MapFrom((source, context) => context.Items[nameof(OrderQueryResult.StopPrice)]))
                .ForCtorParam(nameof(OrderQueryResult.IcebergQuantity), x => x.MapFrom((source, context) => context.Items[nameof(OrderQueryResult.IcebergQuantity)]))
                .ForCtorParam(nameof(OrderQueryResult.Time), x => x.MapFrom(y => y.TransactionTime))
                .ForCtorParam(nameof(OrderQueryResult.UpdateTime), x => x.MapFrom(y => y.TransactionTime))
                .ForCtorParam(nameof(OrderQueryResult.IsWorking), x => x.MapFrom(_ => true))
                .ForCtorParam(nameof(OrderQueryResult.OriginalQuoteOrderQuantity), x => x.MapFrom((source, context) => context.Items[nameof(OrderQueryResult.OriginalQuoteOrderQuantity)]));

            CreateMap<OrderQueryResult, OrderTableParameterEntity>();

            CreateMap<CancelStandardOrderResult, CancelOrderEntity>();
        }
    }
}