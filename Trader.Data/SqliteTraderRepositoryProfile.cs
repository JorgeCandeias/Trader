using AutoMapper;
using Trader.Data.Converters;

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

            CreateMap<OrderGroupEntity, OrderGroup>().ConvertUsing<OrderGroupConverter>();
        }
    }
}