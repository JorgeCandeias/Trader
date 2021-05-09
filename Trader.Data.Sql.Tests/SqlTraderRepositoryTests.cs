using AutoMapper;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace Trader.Data.Sql.Tests
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

        [Fact]
        public async Task GetMaxTradeIdReturnsZeroIfNoTradesExist()
        {
            // arrange
            using var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            var repository = CreateRepository();

            // act
            var result = await repository.GetMaxTradeIdAsync("ZZZ").ConfigureAwait(false);

            // assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetMaxTradeIdReturnsExpectedValue()
        {
            // arrange
            using var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled);
            using var connection = new SqlConnection(ConnectionString);

            await connection
                .ExecuteAsync(@"
                    INSERT INTO [dbo].[Trade]
                    (
                        [Symbol], [Id], [OrderId], [OrderListId], [Price], [Quantity], [QuoteQuantity], [Commission], [CommissionAsset], [Time], [IsBuyer], [IsMaker], [IsBestMatch]
                    )
                    VALUES
                        ('ZZZ', 1, 2, 0, 0, 0, 0, 0, 'AAA', '2021-01-01', 0, 0, 0),
                        ('ZZZ', 2, 2, 0, 0, 0, 0, 0, 'AAA', '2021-01-01', 0, 0, 0),
                        ('ZZZ', 3, 2, 0, 0, 0, 0, 0, 'AAA', '2021-01-01', 0, 0, 0)")
                .ConfigureAwait(false);

            var repository = CreateRepository();

            // act
            var result = await repository.GetMaxTradeIdAsync("ZZZ").ConfigureAwait(false);

            // assert
            Assert.Equal(3, result);
        }
    }
}