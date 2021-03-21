using AutoMapper;

namespace Trader.Data
{
    internal class SqliteTraderRepositoryProfile : Profile
    {
        public SqliteTraderRepositoryProfile()
        {
            CreateMap<OrderQueryResult, OrderEntity>()
                .ReverseMap();

            CreateMap<AccountTrade, TradeEntity>()
                .ReverseMap();
        }
    }
}