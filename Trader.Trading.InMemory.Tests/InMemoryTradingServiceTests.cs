using Moq;
using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.InMemory.Tests
{
    public class InMemoryTradingServiceTests
    {
        [Fact]
        public async Task CancelsOrder()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;
            var cancelled = CancelStandardOrderResult.Empty with { Symbol = symbol, OrderId = 123 };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).CancelOrderAsync(symbol, orderId))
                .ReturnsAsync(cancelled);

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.CancelOrderAsync(symbol, orderId, CancellationToken.None);

            // asert
            Assert.Same(cancelled, result);
        }

        [Fact]
        public async Task CreateOrder()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 1000m;
            var quoteOrderQuantity = 2000m;
            var price = 3000m;
            var newClientOrderId = "ZZZ";
            var stopPrice = 4000m;
            var icebergQuantity = 100m;
            var created = OrderResult.Empty with { Symbol = symbol, OrderId = orderId };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity))
                .ReturnsAsync(created);

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity, CancellationToken.None);

            // asert
            Assert.Same(created, result);
        }

        [Fact]
        public async Task GetsAllOrders()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;
            var limit = 100;

            var orders = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = orderId }
            };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).GetAllOrdersAsync(symbol, orderId, limit))
                .ReturnsAsync(orders);

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.GetAllOrdersAsync(symbol, orderId, limit, CancellationToken.None);

            // asert
            Assert.Same(orders, result);
        }

        [Fact]
        public async Task GetsOpenOrders()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;

            var orders = new[]
            {
                OrderQueryResult.Empty with { Symbol = symbol, OrderId = orderId }
            };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).GetOpenOrdersAsync(symbol))
                .ReturnsAsync(orders);

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.GetOpenOrdersAsync(symbol, CancellationToken.None);

            // asert
            Assert.Same(orders, result);
        }

        [Fact]
        public async Task GetsOrders()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;

            var order = OrderQueryResult.Empty with { Symbol = symbol, OrderId = orderId };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).GetOrderAsync(symbol, orderId, null))
                .ReturnsAsync(order);

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.GetOrderAsync(symbol, orderId, null, CancellationToken.None);

            // asert
            Assert.Same(order, result);
        }

        [Fact]
        public async Task GetsExchangeInfo()
        {
            // arrange
            var symbol = "ABCXYZ";

            var info = ExchangeInfo.Empty with { Symbols = ImmutableList.Create(Symbol.Empty with { Name = symbol }) };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).GetExchangeInfoAsync())
                .ReturnsAsync(info);

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.GetExchangeInfoAsync(CancellationToken.None);

            // asertc
            Assert.Same(info, result);
        }

        [Fact]
        public async Task SetsExchangeInfo()
        {
            // arrange
            var symbol = "ABCXYZ";

            var info = ExchangeInfo.Empty with { Symbols = ImmutableList.Create(Symbol.Empty with { Name = symbol }) };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).SetExchangeInfoAsync(info))
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            await service.SetExchangeInfoAsync(info, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task ClosesUserDataStream()
        {
            // arrange
            var listenKey = "123";

            var factory = Mock.Of<IGrainFactory>();

            var service = new InMemoryTradingService(factory);

            // act
            await service.CloseUserDataStreamAsync(listenKey, CancellationToken.None);

            // assert
            Assert.True(true);
        }

        [Fact]
        public async Task CreatesUserDataStream()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.CreateUserDataStreamAsync(CancellationToken.None);

            // assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Sets24hTickerPriceChangeStatistics()
        {
            // arrange
            var symbol = "ABCXYZ";

            var ticker = Ticker.Empty with { Symbol = symbol };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).Set24hTickerPriceChangeStatisticsAsync(ticker))
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            await service.Set24hTickerPriceChangeStatisticsAsync(ticker, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task Gets24hTickerPriceChangeStatisticsBySymbol()
        {
            // arrange
            var symbol = "ABCXYZ";

            var ticker = Ticker.Empty with { Symbol = symbol };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).Get24hTickerPriceChangeStatisticsAsync(symbol))
                .ReturnsAsync(ticker)
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.Get24hTickerPriceChangeStatisticsAsync(symbol, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
            Assert.Same(ticker, result);
        }

        [Fact]
        public async Task Gets24hTickerPriceChangeStatistics()
        {
            // arrange
            var symbol = "ABCXYZ";

            var ticker = Ticker.Empty with { Symbol = symbol };
            var tickers = new[] { ticker };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).Get24hTickerPriceChangeStatisticsAsync())
                .ReturnsAsync(tickers)
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.Get24hTickerPriceChangeStatisticsAsync(CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
            Assert.Same(tickers, result);
        }
    }
}