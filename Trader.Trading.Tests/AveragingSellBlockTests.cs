using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AveragingSellBlockTests
    {
        [Fact]
        public async Task ThrowsOnNullContext()
        {
            // arrange
            IAlgoContext context = null!;
            var logger = NullLogger<AveragingSellBlock>.Instance;
            var balances = Mock.Of<IBalanceProvider>();
            var savings = Mock.Of<ISavingsProvider>();
            var tickers = Mock.Of<ITickerProvider>();
            var block = new AveragingSellBlock(logger, balances, savings, tickers);

            // act
            Task TestCode() => block.SetAveragingSellAsync(context, null!, null!, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("context", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullSymbol()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var logger = NullLogger<AveragingSellBlock>.Instance;
            var balances = Mock.Of<IBalanceProvider>();
            var savings = Mock.Of<ISavingsProvider>();
            var tickers = Mock.Of<ITickerProvider>();
            var block = new AveragingSellBlock(logger, balances, savings, tickers);

            // act
            Task TestCode() => block.SetAveragingSellAsync(context, null!, null!, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("symbol", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullOrders()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var symbol = Symbol.Empty;
            var logger = NullLogger<AveragingSellBlock>.Instance;
            var balances = Mock.Of<IBalanceProvider>();
            var savings = Mock.Of<ISavingsProvider>();
            var tickers = Mock.Of<ITickerProvider>();
            var block = new AveragingSellBlock(logger, balances, savings, tickers);

            // act
            Task TestCode() => block.SetAveragingSellAsync(context, symbol, null!, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("orders", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNonBuyOrder()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var symbol = Symbol.Empty;
            var orders = new[] { OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Sell } };
            var logger = NullLogger<AveragingSellBlock>.Instance;
            var balances = Mock.Of<IBalanceProvider>();
            var savings = Mock.Of<ISavingsProvider>();
            var tickers = Mock.Of<ITickerProvider>();
            var block = new AveragingSellBlock(logger, balances, savings, tickers);

            // act
            Task TestCode() => block.SetAveragingSellAsync(context, symbol, orders, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNonSignificantOrder()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var symbol = Symbol.Empty;
            var orders = new[] { OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Buy, ExecutedQuantity = 0m } };
            var logger = NullLogger<AveragingSellBlock>.Instance;
            var balances = Mock.Of<IBalanceProvider>();
            var savings = Mock.Of<ISavingsProvider>();
            var tickers = Mock.Of<ITickerProvider>();
            var block = new AveragingSellBlock(logger, balances, savings, tickers);

            // act
            Task TestCode() => block.SetAveragingSellAsync(context, symbol, orders, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }
    }
}