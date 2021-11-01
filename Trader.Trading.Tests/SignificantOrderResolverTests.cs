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
    public class SignificantOrderResolverTests
    {
        [Fact]
        public async Task ResolvesEmpty()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var logger = NullLogger<SignificantOrderResolver>.Instance;

            var orders = Array.Empty<OrderQueryResult>();
            var orderProvider = Mock.Of<IOrderProvider>();
            Mock.Get(orderProvider)
                .Setup(x => x.GetOrdersByFilterAsync(symbol.Name, null, false, true, CancellationToken.None))
                .ReturnsAsync(orders)
                .Verifiable();

            var trades = Array.Empty<AccountTrade>();
            var tradeProvider = Mock.Of<ITradeProvider>();
            Mock.Get(tradeProvider)
                .Setup(x => x.GetTradesAsync(symbol.Name, CancellationToken.None))
                .ReturnsAsync(trades)
                .Verifiable();

            var resolver = new SignificantOrderResolver(logger, orderProvider, tradeProvider);

            // act
            var result = await resolver.ResolveAsync(symbol, CancellationToken.None);

            // assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Empty(result.Orders);
            Assert.Empty(result.ProfitEvents);
            Assert.Empty(result.CommissionEvents);
        }

        [Fact]
        public async Task ResolvesScenario()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var logger = NullLogger<SignificantOrderResolver>.Instance;

            var order1 = OrderQueryResult.Empty with
            {
                Symbol = symbol.Name,
                OrderId = 1,
                Side = OrderSide.Buy,
                ExecutedQuantity = 100m
            };
            var order2 = OrderQueryResult.Empty with
            {
                Symbol = symbol.Name,
                OrderId = 2,
                Side = OrderSide.Sell,
                ExecutedQuantity = 100m
            };
            var orders = new[] { order1, order2 };
            var orderProvider = Mock.Of<IOrderProvider>();
            Mock.Get(orderProvider)
                .Setup(x => x.GetOrdersByFilterAsync(symbol.Name, null, false, true, CancellationToken.None))
                .ReturnsAsync(orders)
                .Verifiable();

            var trade1 = AccountTrade.Empty with
            {
                Id = 1,
                Symbol = symbol.Name,
                OrderId = 1,
                Price = 100m,
                Quantity = 10m,
                Commission = 0m
            };
            var trade2 = AccountTrade.Empty with
            {
                Id = 2,
                Symbol = symbol.Name,
                OrderId = 2,
                Price = 110m,
                Quantity = 10m,
                Commission = 0m
            };

            var trades = new[] { trade1, trade2 };
            var tradeProvider = Mock.Of<ITradeProvider>();
            Mock.Get(tradeProvider)
                .Setup(x => x.GetTradesAsync(symbol.Name, CancellationToken.None))
                .ReturnsAsync(trades)
                .Verifiable();

            var resolver = new SignificantOrderResolver(logger, orderProvider, tradeProvider);

            // act
            var result = await resolver.ResolveAsync(symbol, CancellationToken.None);

            // assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Empty(result.Orders);
            Assert.Collection(result.ProfitEvents, x => Assert.Equal(10m, x.Quantity));
            Assert.Collection(result.CommissionEvents,
                x => Assert.Equal(0m, x.Commission),
                x => Assert.Equal(0m, x.Commission));
        }
    }
}