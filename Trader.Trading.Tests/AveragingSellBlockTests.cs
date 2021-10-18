using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
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

            // act
            Task TestCode() => context.SetAveragingSellAsync(null!, null!, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("context", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullSymbol()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();

            // act
            Task TestCode() => context.SetAveragingSellAsync(null!, null!, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("symbol", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullOrders()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();
            var symbol = Symbol.Empty;

            // act
            Task TestCode() => context.SetAveragingSellAsync(symbol, null!, 0m, false, CancellationToken.None).AsTask();

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

            // act
            Task TestCode() => context.SetAveragingSellAsync(symbol, orders, 0m, false, CancellationToken.None).AsTask();

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

            // act
            Task TestCode() => context.SetAveragingSellAsync(symbol, orders, 0m, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }
    }
}