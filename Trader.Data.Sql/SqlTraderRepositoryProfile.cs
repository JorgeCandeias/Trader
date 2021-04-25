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

            CreateMap<CancelStandardOrderResult, OrderQueryResult>();

            CreateMap<OrderResult, OrderQueryResult>();
        }
    }
}