using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
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

            // act
            Task TestCode() => context.SetAveragingSellAsync(null!, null!, 0m, false, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>("context", TestCode);
        }

        [Fact]
        public async Task ThrowsOnNullSymbol()
        {
            // arrange
            var context = Mock.Of<IAlgoContext>();

            // act
            Task TestCode() => context.SetAveragingSellAsync(null!, null!, 0m, false, false, CancellationToken.None).AsTask();

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
            Task TestCode() => context.SetAveragingSellAsync(symbol, null!, 0m, false, false, CancellationToken.None).AsTask();

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
            Task TestCode() => context.SetAveragingSellAsync(symbol, orders, 0m, false, false, CancellationToken.None).AsTask();

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
            Task TestCode() => context.SetAveragingSellAsync(symbol, orders, 0m, false, false, CancellationToken.None).AsTask();

            // assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>("orders", TestCode);
        }

        [Fact]
        public async Task HandlesNoTicker()
        {
            // arrange
            var transient = ImmutableSortedOrderSet.Create(new[] { OrderQueryResult.Empty with { OrderId = 999, Side = OrderSide.Sell, Symbol = "ABCXYZ" } });

            var repository = Mock.Of<ITradingRepository>(x =>
                x.GetTransientOrdersBySideAsync("ABCXYZ", OrderSide.Sell, CancellationToken.None) == Task.FromResult(transient));

            var trader = Mock.Of<ITradingService>();

            var provider = new ServiceCollection()
                .AddSingleton(repository)
                .AddSingleton(Mock.Of<ISavingsProvider>())
                .AddSingleton(Mock.Of<ITickerProvider>())
                .AddSingleton(Mock.Of<ILogger<IAlgoContext>>())
                .AddSingleton(trader)
                .BuildServiceProvider();

            var context = Mock.Of<IAlgoContext>(x => x.ServiceProvider == provider);

            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var orders = new[] {
                OrderQueryResult.Empty with { OrderId = 123, Side = OrderSide.Buy, ExecutedQuantity = 1000m },
                OrderQueryResult.Empty with { OrderId = 234, Side = OrderSide.Buy, ExecutedQuantity = 2000m },
                OrderQueryResult.Empty with { OrderId = 345, Side = OrderSide.Buy, ExecutedQuantity = 3000m }
            };

            // act
            await context.SetAveragingSellAsync(symbol, orders, 0m, false, false, CancellationToken.None);

            // assert
            Mock.Get(trader).Verify(x => x.CancelOrderAsync("ABCXYZ", 999, CancellationToken.None));
        }

        [Fact]
        public async Task HandlesNoOrders()
        {
            // arrange
            var transient = ImmutableSortedOrderSet.Create(new[] { OrderQueryResult.Empty with { OrderId = 999, Side = OrderSide.Sell, Symbol = "ABCXYZ" } });

            var repository = Mock.Of<ITradingRepository>(x =>
                x.GetTransientOrdersBySideAsync("ABCXYZ", OrderSide.Sell, CancellationToken.None) == Task.FromResult(transient));

            var trader = Mock.Of<ITradingService>();

            var provider = new ServiceCollection()
                .AddSingleton(repository)
                .AddSingleton(Mock.Of<ISavingsProvider>())
                .AddSingleton(Mock.Of<ITickerProvider>())
                .AddSingleton(Mock.Of<ILogger<IAlgoContext>>())
                .AddSingleton(trader)
                .BuildServiceProvider();

            var context = Mock.Of<IAlgoContext>(x => x.ServiceProvider == provider);

            var symbol = Symbol.Empty with { Name = "ABCXYZ", BaseAsset = "ABC", QuoteAsset = "XYZ" };

            var orders = Array.Empty<OrderQueryResult>();

            // act
            await context.SetAveragingSellAsync(symbol, orders, 0m, false, false, CancellationToken.None);

            // assert
            Mock.Get(trader).Verify(x => x.CancelOrderAsync("ABCXYZ", 999, CancellationToken.None));
        }
    }
}