using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Operations;
using Outcompute.Trader.Trading.Operations.AveragingSell;
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
        public async Task ThrowsOnNullSymbol()
        {
            // arrange
            var block = new AveragingSellOperation(
                NullLogger<AveragingSellOperation>.Instance,
                Mock.Of<IBalanceProvider>(),
                Mock.Of<ISavingsProvider>(),
                Mock.Of<ITickerProvider>(),
                Mock.Of<IEnsureSingleOrderOperation>(),
                Mock.Of<IClearOpenOrdersOperation>());

            // act
            Task TestCode() => block.SetAveragingSellAsync(null!, null!, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("symbol", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullOrders()
        {
            // arrange
            var symbol = Symbol.Empty;
            var block = new AveragingSellOperation(
                NullLogger<AveragingSellOperation>.Instance,
                Mock.Of<IBalanceProvider>(),
                Mock.Of<ISavingsProvider>(),
                Mock.Of<ITickerProvider>(),
                Mock.Of<IEnsureSingleOrderOperation>(),
                Mock.Of<IClearOpenOrdersOperation>());

            // act
            Task TestCode() => block.SetAveragingSellAsync(symbol, null!, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("orders", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNonBuyOrder()
        {
            // arrange
            var symbol = Symbol.Empty;
            var orders = new[] { OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Sell } };
            var block = new AveragingSellOperation(
                NullLogger<AveragingSellOperation>.Instance,
                Mock.Of<IBalanceProvider>(),
                Mock.Of<ISavingsProvider>(),
                Mock.Of<ITickerProvider>(),
                Mock.Of<IEnsureSingleOrderOperation>(),
                Mock.Of<IClearOpenOrdersOperation>());

            // act
            Task TestCode() => block.SetAveragingSellAsync(symbol, orders, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNonSignificantOrder()
        {
            // arrange
            var symbol = Symbol.Empty;
            var orders = new[] { OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Buy, ExecutedQuantity = 0m } };
            var block = new AveragingSellOperation(
                NullLogger<AveragingSellOperation>.Instance,
                Mock.Of<IBalanceProvider>(),
                Mock.Of<ISavingsProvider>(),
                Mock.Of<ITickerProvider>(),
                Mock.Of<IEnsureSingleOrderOperation>(),
                Mock.Of<IClearOpenOrdersOperation>());

            // act
            Task TestCode() => block.SetAveragingSellAsync(symbol, orders, 0m, false, CancellationToken.None);

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }
    }
}