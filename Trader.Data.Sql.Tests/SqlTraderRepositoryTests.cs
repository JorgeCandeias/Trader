using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Outcompute.Trader.Data.Sql.Tests
{
    public class SqlTraderRepositoryTests
    {
        private const string ConnectionString = @"server=(localdb)\mssqllocaldb;database=TraderTest";

        private static readonly IMapper _mapper = new MapperConfiguration(options =>
        {
            options.AddProfile<SqlTradingRepositoryProfile>();
        }).CreateMapper();

        private static SqlTradingRepository CreateRepository()
        {
            return new SqlTradingRepository(
                Options.Create(new SqlTradingRepositoryOptions { ConnectionString = ConnectionString }),
                NullLogger<SqlTradingRepository>.Instance,
                _mapper);
        }
    }
}