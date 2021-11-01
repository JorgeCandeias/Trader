using Moq;
using Orleans;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
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

        [Fact]
        public async Task GetsAccountInfo()
        {
            // arrange
            var info = AccountInfo.Empty with { AccountType = AccountType.Spot };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).GetAccountInfoAsync())
                .ReturnsAsync(info)
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.GetAccountInfoAsync(CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
            Assert.Same(info, result);
        }

        [Fact]
        public async Task SetsAccountInfo()
        {
            // arrange
            var info = AccountInfo.Empty with { AccountType = AccountType.Spot };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).SetAccountInfoAsync(info))
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            await service.SetAccountInfoAsync(info, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task SetsAccountTrade()
        {
            // arrange
            var trade = AccountTrade.Empty with { Symbol = "ABCXYZ", Id = 123 };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).SetAccountTradeAsync(trade))
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            await service.SetAccountTradeAsync(trade, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsAccountTrades()
        {
            // arrange
            var symbol = "ABCXYZ";
            var fromId = 123;
            var limit = 1000;
            var trade = AccountTrade.Empty with { Symbol = symbol, Id = 123 };
            var trades = ImmutableSortedTradeSet.Create(new[] { trade });

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).GetAccountTradesAsync(symbol, fromId, limit))
                .ReturnsAsync(trades)
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.GetAccountTradesAsync(symbol, fromId, limit, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
            Assert.Same(trades, result);
        }

        [Fact]
        public void GetsCachedFlexibleProductsByAsset()
        {
            // arrange
            var asset = "ABC";

            var factory = Mock.Of<IGrainFactory>();

            var service = new InMemoryTradingService(factory);

            // act
            void Test() => service.GetCachedFlexibleProductsByAsset(asset);

            // assert
            Assert.Throws<NotImplementedException>(Test);
        }

        [Fact]
        public void GetsFlexibleProductList()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();
            var service = new InMemoryTradingService(factory);

            // act
            Task Test() => service.GetFlexibleProductListAsync(SavingsStatus.None, SavingsFeatured.None, null, null, CancellationToken.None);

            // assert
            Assert.ThrowsAsync<NotImplementedException>(Test);
        }

        [Fact]
        public void GetsFlexibleProductPositions()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();
            var service = new InMemoryTradingService(factory);

            // act
            Task Test() => service.GetFlexibleProductPositionsAsync("ABC", CancellationToken.None);

            // assert
            Assert.ThrowsAsync<NotImplementedException>(Test);
        }

        [Fact]
        public void GetsKlines()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();
            var service = new InMemoryTradingService(factory);

            // act
            Task Test() => service.GetKlinesAsync("ABCXYZ", KlineInterval.None, DateTime.MinValue, DateTime.MinValue, 0, CancellationToken.None);

            // assert
            Assert.ThrowsAsync<NotImplementedException>(Test);
        }

        [Fact]
        public void GetsSymbolPriceTicker()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();
            var service = new InMemoryTradingService(factory);

            // act
            Task Test() => service.GetSymbolPriceTickerAsync("ABCXYZ", CancellationToken.None);

            // assert
            Assert.ThrowsAsync<NotImplementedException>(Test);
        }

        [Fact]
        public void GetsSymbolPriceTickers()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();
            var service = new InMemoryTradingService(factory);

            // act
            Task Test() => service.GetSymbolPriceTickersAsync(CancellationToken.None);

            // assert
            Assert.ThrowsAsync<NotImplementedException>(Test);
        }

        [Fact]
        public void PingsUserDataStream()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();
            var service = new InMemoryTradingService(factory);

            // act
            Task Test() => service.PingUserDataStreamAsync("ZZZ", CancellationToken.None);

            // assert
            Assert.ThrowsAsync<NotImplementedException>(Test);
        }

        [Fact]
        public void RedeemsFlexibleProduct()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();
            var service = new InMemoryTradingService(factory);

            // act
            Task Test() => service.RedeemFlexibleProductAsync("P1", 0, SavingsRedemptionType.None, CancellationToken.None);

            // assert
            Assert.ThrowsAsync<NotImplementedException>(Test);
        }

        [Fact]
        public async Task SetsFlexibleProductPositions()
        {
            // arrange
            var position = SavingsPosition.Empty with { Asset = "ABC" };
            var positions = new[] { position };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).SetFlexibleProductPositionsAsync(positions))
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            await service.SetFlexibleProductPositionsAsync(positions);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task SetsLeftDailyRedemptionQuotaOnFlexibleProduct()
        {
            // arrange
            var productId = "P1";
            var type = SavingsRedemptionType.Fast;
            var item = SavingsQuota.Empty with { Asset = "ABC" };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, item))
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            await service.SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, item);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task TryGetLeftDailyRedemptionQuotaOnFlexibleProduct()
        {
            // arrange
            var productId = "P1";
            var type = SavingsRedemptionType.Fast;
            var quota = SavingsQuota.Empty with { Asset = "ABC" };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<IInMemoryTradingServiceGrain>(Guid.Empty, null).TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type))
                .ReturnsAsync(quota)
                .Verifiable();

            var service = new InMemoryTradingService(factory);

            // act
            var result = await service.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
            Assert.Same(quota, result);
        }

        [Fact]
        public void WithBackoff()
        {
            // arrange
            var factory = Mock.Of<IGrainFactory>();

            var service = new InMemoryTradingService(factory);

            // act
            var result = service.WithBackoff();

            // assert
            Mock.Get(factory).VerifyAll();
            Assert.Same(service, result);
        }
    }
}