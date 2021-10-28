using Moq;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Models;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.InMemory.Tests
{
    public class InMemoryTradingServiceGrainTests
    {
        [Fact]
        public async Task CancelsOrder()
        {
            // arrange
            var clock = Mock.Of<ISystemClock>();
            var service = new InMemoryTradingServiceGrain(clock);
            var symbol = "ABCXYZ";
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 234m;
            var quoteOrderQuantity = 345m;
            var price = 456m;
            var newClientOrderId = "XYZ";
            var stopPrice = 333m;
            var icebergQuantity = 10m;

            // act
            var added = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            var result = await service.CancelOrderAsync(symbol, added.OrderId);

            // assert
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(added.OrderId, result.OrderId);
            Assert.Equal(OrderStatus.Canceled, result.Status);
        }

        [Fact]
        public async Task CreatesOrder()
        {
            // arrange
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == DateTime.UtcNow);
            var service = new InMemoryTradingServiceGrain(clock);
            var symbol = "ABCXYZ";
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 234m;
            var quoteOrderQuantity = 345m;
            var price = 456m;
            var newClientOrderId = "XYZ";
            var stopPrice = 333m;
            var icebergQuantity = 10m;

            // act
            var result = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);

            // assert
            Assert.Equal(symbol, result.Symbol);
            Assert.NotEqual(0, result.OrderId);
            Assert.Equal(0, result.OrderListId);
            Assert.Equal(newClientOrderId, result.ClientOrderId);
            Assert.Equal(clock.UtcNow, result.TransactionTime);
            Assert.Equal(price, result.Price);
            Assert.Equal(quantity, result.OriginalQuantity);
            Assert.Equal(0, result.ExecutedQuantity);
            Assert.Equal(quoteOrderQuantity, result.CummulativeQuoteQuantity);
            Assert.Equal(OrderStatus.New, result.Status);
            Assert.Equal(timeInForce, result.TimeInForce);
            Assert.Equal(type, result.Type);
            Assert.Equal(side, result.Side);
            Assert.Equal(ImmutableList<OrderFill>.Empty, result.Fills);
        }

        [Fact]
        public async Task GetsAllOrders()
        {
            // arrange
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == DateTime.UtcNow);
            var service = new InMemoryTradingServiceGrain(clock);
            var symbol = "ABCXYZ";
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 234m;
            var quoteOrderQuantity = 345m;
            var price = 456m;
            var newClientOrderId = "XYZ";
            var stopPrice = 333m;
            var icebergQuantity = 10m;

            // act
            var order1 = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            var order2 = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            var order3 = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            var result = await service.GetAllOrdersAsync(symbol, 0, 1000);

            // assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.OrderId == order1.OrderId);
            Assert.Contains(result, x => x.OrderId == order2.OrderId);
            Assert.Contains(result, x => x.OrderId == order3.OrderId);
        }

        [Fact]
        public async Task GetsOpenOrders()
        {
            // arrange
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == DateTime.UtcNow);
            var service = new InMemoryTradingServiceGrain(clock);
            var symbol = "ABCXYZ";
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 234m;
            var quoteOrderQuantity = 345m;
            var price = 456m;
            var newClientOrderId = "XYZ";
            var stopPrice = 333m;
            var icebergQuantity = 10m;

            // act
            var order1 = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            var order2 = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            var order3 = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            await service.CancelOrderAsync(symbol, order3.OrderId);
            var result = await service.GetOpenOrdersAsync(symbol);

            // assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.OrderId == order1.OrderId);
            Assert.Contains(result, x => x.OrderId == order2.OrderId);
            Assert.DoesNotContain(result, x => x.OrderId == order3.OrderId);
        }

        [Fact]
        public async Task GetsOrder()
        {
            // arrange
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == DateTime.UtcNow);
            var service = new InMemoryTradingServiceGrain(clock);
            var symbol = "ABCXYZ";
            var side = OrderSide.Buy;
            var type = OrderType.Limit;
            var timeInForce = TimeInForce.GoodTillCanceled;
            var quantity = 234m;
            var quoteOrderQuantity = 345m;
            var price = 456m;
            var newClientOrderId = "XYZ";
            var stopPrice = 333m;
            var icebergQuantity = 10m;

            // act
            var order1 = await service.CreateOrderAsync(symbol, side, type, timeInForce, quantity, quoteOrderQuantity, price, newClientOrderId, stopPrice, icebergQuantity);
            var result = await service.GetOrderAsync(symbol, order1.OrderId, null);

            // assert
            Assert.Equal(symbol, result.Symbol);
            Assert.Equal(order1.OrderId, result.OrderId);
        }

        [Fact]
        public async Task SetsAndGetsFlexibleProductPositions()
        {
            // arrange
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == DateTime.UtcNow);
            var service = new InMemoryTradingServiceGrain(clock);
            var position = SavingsPosition.Empty with { Asset = "ABC", FreeAmount = 123m };

            // act
            await service.SetFlexibleProductPositionsAsync(new[] { position });
            var result = await service.GetFlexibleProductPositionsAsync(position.Asset);

            // assert
            var single = Assert.Single(result);
            Assert.Equal(position.Asset, single.Asset);
            Assert.Equal(position.FreeAmount, single.FreeAmount);
        }

        [Fact]
        public async Task SetsAndGetsLeftDailyRedemptionQuotaOnFlexibleProduct()
        {
            // arrange
            var clock = Mock.Of<ISystemClock>(x => x.UtcNow == DateTime.UtcNow);
            var service = new InMemoryTradingServiceGrain(clock);
            var productId = "PABC";
            var quota = SavingsQuota.Empty with { Asset = "ABC", LeftQuota = 123m };

            // act
            await service.SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, SavingsRedemptionType.Fast, quota);
            var result = await service.TryGetLeftDailyRedemptionQuotaOnFlexibleProductAsync(productId, SavingsRedemptionType.Fast);

            // assert
            Assert.NotNull(result);
            Assert.Equal(quota.Asset, result!.Asset);
            Assert.Equal(quota.LeftQuota, result.LeftQuota);
        }
    }
}