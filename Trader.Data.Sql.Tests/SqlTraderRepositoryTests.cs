using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Models;
using System;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

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

        private readonly SqlTradingRepository _repository;

        public SqlTraderRepositoryTests()
        {
            _repository = CreateRepository();
        }

        [Fact]
        public async Task SetsAndGetsBalances()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // arrange
            var balance1 = new Balance(Guid.NewGuid().ToString(), 111, 222, DateTime.UtcNow);
            var balance2 = new Balance(Guid.NewGuid().ToString(), 333, 444, DateTime.UtcNow);
            var balances = new[] { balance1, balance2 };

            // act
            await _repository.SetBalancesAsync(balances);
            var result1 = await _repository.TryGetBalanceAsync(balance1.Asset);
            var result2 = await _repository.TryGetBalanceAsync(balance2.Asset);
            var result3 = await _repository.TryGetBalanceAsync(Guid.NewGuid().ToString());

            // assert
            Assert.Equal(balance1, result1);
            Assert.Equal(balance2, result2);
            Assert.Null(result3);
        }

        [Fact]
        public async Task SetsAndGetsOrder()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // arrange
            var order = new OrderQueryResult(Guid.NewGuid().ToString(), 1, 0, string.Empty, 123, 1000, 0, 0, OrderStatus.New, TimeInForce.GoodTillCanceled, OrderType.Limit, OrderSide.Buy, 0, 0, DateTime.UtcNow, DateTime.UtcNow, false, 0);

            // act
            await _repository.SetOrderAsync(order);
            var result = await _repository.GetOrdersAsync(order.Symbol);

            // assert
            Assert.Collection(result, x =>
            {
                Assert.Equal(order.Symbol, x.Symbol);
                Assert.Equal(order.OrderId, x.OrderId);
            });
        }

        [Fact]
        public async Task SetsAndGetsOrders()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // arrange
            var order = new OrderQueryResult(Guid.NewGuid().ToString(), 1, 0, string.Empty, 123, 1000, 0, 0, OrderStatus.New, TimeInForce.GoodTillCanceled, OrderType.Limit, OrderSide.Buy, 0, 0, DateTime.UtcNow, DateTime.UtcNow, false, 0);

            // act
            await _repository.SetOrdersAsync(new[] { order });
            var result = await _repository.GetOrdersAsync(order.Symbol);

            // assert
            Assert.Collection(result, x =>
            {
                Assert.Equal(order.Symbol, x.Symbol);
                Assert.Equal(order.OrderId, x.OrderId);
            });
        }

        [Fact]
        public async Task SetsAndGetsTrade()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // arrange
            var trade = new AccountTrade(Guid.NewGuid().ToString(), 1, 2, 0, 123, 1000, 0, 0.01m, Guid.NewGuid().ToString(), DateTime.UtcNow, true, true, true);

            // act
            await _repository.SetTradeAsync(trade);
            var result = await _repository.GetTradesAsync(trade.Symbol);

            // assert
            Assert.Collection(result, x =>
            {
                Assert.Equal(trade.Symbol, x.Symbol);
                Assert.Equal(trade.Id, x.Id);
            });
        }

        [Fact]
        public async Task SetsAndGetsTrades()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // arrange
            var trade = new AccountTrade(Guid.NewGuid().ToString(), 1, 2, 0, 123, 1000, 0, 0.01m, Guid.NewGuid().ToString(), DateTime.UtcNow, true, true, true);

            // act
            await _repository.SetTradesAsync(new[] { trade });
            var result = await _repository.GetTradesAsync(trade.Symbol);

            // assert
            Assert.Collection(result, x =>
            {
                Assert.Equal(trade.Symbol, x.Symbol);
                Assert.Equal(trade.Id, x.Id);
            });
        }

        [Fact]
        public async Task SetsAndGetsTicker()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // arrange
            var ticker = new MiniTicker(Guid.NewGuid().ToString(), DateTime.Today, 1.0m, 2.0m, 3.0m, 4.0m, 5.0m, 6.0m);

            // act
            await _repository.SetTickerAsync(ticker);
            var result = await _repository.TryGetTickerAsync(ticker.Symbol);

            // assert
            Assert.NotNull(result);
            Assert.Equal(ticker, result);
        }

        [Fact]
        public async Task SetsAndGetsKline()
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // arrange
            var kline = new Kline(Guid.NewGuid().ToString(), KlineInterval.Days1, DateTime.Today, DateTime.Today.AddDays(-1), DateTime.Today, 1, 3, 1m, 2m, 3m, 4m, 5m, 6m, 3, true, 7m, 8m);

            // act
            await _repository.SetKlineAsync(kline);
            var result = await _repository.GetKlinesAsync(kline.Symbol, kline.Interval, DateTime.Today.AddDays(-1), DateTime.Today);

            // assert
            Assert.Collection(result, x => Assert.Equal(kline, x));
        }
    }
}