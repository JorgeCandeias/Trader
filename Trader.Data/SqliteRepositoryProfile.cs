using AutoMapper;

namespace Trader.Data
{
    internal class SqliteRepositoryProfile : Profile
    {
        public SqliteRepositoryProfile()
        {
            CreateMap<OrderQueryResult, OrderEntity>()
                .ReverseMap();
        }
    }
}