using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Binance.Tests
{
    public class BinanceTradingServiceWithBackoffTests
    {
        [Fact]
        public void WithBackoff()
        {
            // arrange
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var trader = Mock.Of<ITradingService>();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = service.WithBackoff();

            // assert
            Assert.Same(service, result);
        }

        [Fact]
        public async Task CancelOrderAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var cancelled = CancelStandardOrderResult.Empty with { Symbol = symbol, OrderId = orderId };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.CancelOrderAsync(symbol, orderId, CancellationToken.None))
                .Returns(Task.FromResult(cancelled))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.CancelOrderAsync(symbol, orderId);

            // assert
            Assert.Equal(cancelled, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task CloseUserDataStreamAsync()
        {
            // arrange
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var listenKey = "ABCD";
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.CloseUserDataStreamAsync(listenKey, CancellationToken.None))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            await service.CloseUserDataStreamAsync(listenKey);

            // assert
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task CreateOrderAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var created = OrderResult.Empty with { Symbol = symbol, OrderId = orderId };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.CreateOrderAsync(symbol, side, type, null, null, null, null, null, null, null, CancellationToken.None))
                .Returns(Task.FromResult(created))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.CreateOrderAsync(symbol, side, type, null, null, null, null, null, null, null);

            // assert
            Assert.Equal(created, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task CreateUserDataStreamAsync()
        {
            // arrange
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var value = "ZZZZ";
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.CreateUserDataStreamAsync(CancellationToken.None))
                .Returns(Task.FromResult(value))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.CreateUserDataStreamAsync();

            // assert
            Assert.Equal(value, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task Get24hTickerPriceChangeStatisticsAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var ticker = Ticker.Empty with { Symbol = symbol, Count = 1 };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.Get24hTickerPriceChangeStatisticsAsync(symbol, CancellationToken.None))
                .Returns(Task.FromResult(ticker))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.Get24hTickerPriceChangeStatisticsAsync(symbol);

            // assert
            Assert.Equal(ticker, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetAccountInfoAsync()
        {
            // arrange
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var info = AccountInfo.Empty with { AccountType = AccountType.Spot };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetAccountInfoAsync(CancellationToken.None))
                .Returns(Task.FromResult(info))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetAccountInfoAsync();

            // assert
            Assert.Equal(info, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetAccountTradesAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var fromId = 123;
            var limit = 234;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var trades = ImmutableSortedTradeSet.Empty;
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetAccountTradesAsync(symbol, fromId, limit, CancellationToken.None))
                .Returns(Task.FromResult(trades))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetAccountTradesAsync(symbol, fromId, limit);

            // assert
            Assert.Equal(trades, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetAllOrdersAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var orderId = 123;
            var limit = 234;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var orders = (IReadOnlyCollection<OrderQueryResult>)ImmutableList<OrderQueryResult>.Empty;
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetAllOrdersAsync(symbol, orderId, limit, CancellationToken.None))
                .Returns(Task.FromResult(orders))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetAllOrdersAsync(symbol, orderId, limit);

            // assert
            Assert.Equal(orders, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetExchangeInfoAsync()
        {
            // arrange
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var info = ExchangeInfo.Empty with { ServerTime = DateTime.UtcNow };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetExchangeInfoAsync(CancellationToken.None))
                .Returns(Task.FromResult(info))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetExchangeInfoAsync();

            // assert
            Assert.Equal(info, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetFlexibleProductListAsync()
        {
            // arrange
            var status = SavingsStatus.Subscribable;
            var featured = SavingsFeatured.All;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var savings = (IReadOnlyCollection<SavingsProduct>)ImmutableList<SavingsProduct>.Empty;
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetSavingsProductsAsync(status, featured, null, null, CancellationToken.None))
                .Returns(Task.FromResult(savings))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetSavingsProductsAsync(status, featured, null, null);

            // assert
            Assert.Equal(savings, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetFlexibleProductPositionsAsync()
        {
            // arrange
            var asset = "ABC";
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var savings = (IReadOnlyCollection<SavingsBalance>)ImmutableList<SavingsBalance>.Empty;
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetSavingsBalancesAsync(asset, CancellationToken.None))
                .Returns(Task.FromResult(savings))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetSavingsBalancesAsync(asset);

            // assert
            Assert.Equal(savings, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetKlinesAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var interval = KlineInterval.Days1;
            var startTime = DateTime.MinValue;
            var endTime = DateTime.MaxValue;
            var limit = 123;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var klines = (IReadOnlyCollection<Kline>)ImmutableList<Kline>.Empty;
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetKlinesAsync(symbol, interval, startTime, endTime, limit, CancellationToken.None))
                .Returns(Task.FromResult(klines))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetKlinesAsync(symbol, interval, startTime, endTime, limit);

            // assert
            Assert.Equal(klines, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetOpenOrdersAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var klines = (IReadOnlyCollection<OrderQueryResult>)ImmutableList<OrderQueryResult>.Empty;
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetOpenOrdersAsync(symbol, CancellationToken.None))
                .Returns(Task.FromResult(klines))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetOpenOrdersAsync(symbol);

            // assert
            Assert.Equal(klines, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetOrderAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var returned = OrderQueryResult.Empty with { Symbol = symbol };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetOrderAsync(symbol, null, null, CancellationToken.None))
                .Returns(Task.FromResult(returned))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetOrderAsync(symbol, null, null);

            // assert
            Assert.Equal(returned, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task GetSymbolPriceTickerAsync()
        {
            // arrange
            var symbol = "ABCXYZ";
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var returned = SymbolPriceTicker.Empty with { Symbol = symbol };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.GetSymbolPriceTickerAsync(symbol, CancellationToken.None))
                .Returns(Task.FromResult(returned))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.GetSymbolPriceTickerAsync(symbol);

            // assert
            Assert.Equal(returned, result);
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task PingUserDataStreamAsync()
        {
            // arrange
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var listenKey = "ZZZZ";
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.PingUserDataStreamAsync(listenKey, CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            await service.PingUserDataStreamAsync(listenKey);

            // assert
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task RedeemFlexibleProductAsync()
        {
            // arrange
            var productId = "ABC";
            var amount = 123m;
            var type = SavingsRedemptionType.Fast;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.RedeemFlexibleProductAsync(productId, amount, type, CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            await service.RedeemFlexibleProductAsync(productId, amount, type);

            // assert
            Mock.Get(trader).VerifyAll();
        }

        [Fact]
        public async Task TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync()
        {
            // arrange
            var productId = "ABC";
            var type = SavingsRedemptionType.Fast;
            var logger = NullLogger<BinanceTradingServiceWithBackoff>.Instance;
            var returned = SavingsQuota.Empty with { Asset = "ABC" };
            var trader = Mock.Of<ITradingService>();
            Mock.Get(trader)
                .Setup(x => x.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type, CancellationToken.None))
                .Returns(Task.FromResult<SavingsQuota?>(returned))
                .Verifiable();
            var service = new BinanceTradingServiceWithBackoff(logger, trader);

            // act
            var result = await service.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, type);

            // assert
            Assert.Equal(returned, result);
            Mock.Get(trader).VerifyAll();
        }
    }
}