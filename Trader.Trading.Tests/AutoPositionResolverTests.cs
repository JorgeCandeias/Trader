using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Outcompute.Trader.Trading.Providers;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AutoPositionResolverTests
    {
        [Fact]
        public void ResolvesEmpty()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };

            var startTime = DateTime.MinValue;
            var logger = NullLogger<AutoPositionResolver>.Instance;
            var orders = OrderCollection.Empty;
            var trades = Array.Empty<AccountTrade>();
            var resolver = new AutoPositionResolver(logger);

            // act
            var result = resolver.Resolve(symbol, orders, trades, startTime);

            // assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Empty(result.Positions);
            Assert.Empty(result.ProfitEvents);
            Assert.Empty(result.CommissionEvents);
        }

        [Fact]
        public void ResolvesScenario()
        {
            // arrange
            var symbol = Symbol.Empty with
            {
                Name = "ABCXYZ",
                BaseAsset = "ABC",
                QuoteAsset = "XYZ"
            };
            var startTime = DateTime.MinValue;

            var logger = NullLogger<AutoPositionResolver>.Instance;

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

            var orders = new OrderCollection(new[] { order1, order2 });

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

            var trades = new TradeCollection(new[] { trade1, trade2 });
            var tradeProvider = Mock.Of<ITradeProvider>();
            Mock.Get(tradeProvider)
                .Setup(x => x.GetTradesAsync(symbol.Name, CancellationToken.None))
                .ReturnsAsync(trades)
                .Verifiable();

            var resolver = new AutoPositionResolver(logger);

            // act
            var result = resolver.Resolve(symbol, orders, trades, startTime);

            // assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Empty(result.Positions);
            Assert.Collection(result.ProfitEvents, x => Assert.Equal(10m, x.Quantity));
            Assert.Collection(result.CommissionEvents,
                x => Assert.Equal(0m, x.Commission),
                x => Assert.Equal(0m, x.Commission));
        }
    }
}