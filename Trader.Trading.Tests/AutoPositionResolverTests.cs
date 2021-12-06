using Outcompute.Trader.Trading.Algorithms.Positions;

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
            var orders = ImmutableSortedSet<OrderQueryResult>.Empty;
            var trades = TradeCollection.Empty;
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

            var orders = ImmutableSortedSet.Create(OrderQueryResult.KeyComparer, new[] { order1, order2 });

            var trade1 = AccountTrade.Empty with
            {
                Id = 1,
                Symbol = symbol.Name,
                OrderId = 1,
                IsBuyer = true,
                Price = 100m,
                Quantity = 100m,
                Commission = 0.1m,
                CommissionAsset = symbol.BaseAsset,
                Time = DateTime.UtcNow - TimeSpan.FromMinutes(1)
            };

            var trade2 = AccountTrade.Empty with
            {
                Id = 2,
                Symbol = symbol.Name,
                OrderId = 2,
                IsBuyer = false,
                Price = 110m,
                Quantity = 100m,
                Commission = 0.1m,
                CommissionAsset = symbol.QuoteAsset,
                Time = DateTime.UtcNow
            };

            var profit = ((trade2.Quantity - trade1.Commission) * trade2.Price) - ((trade1.Quantity - trade1.Commission) * trade1.Price);

            var trades = new TradeCollection(new[] { trade1, trade2 });

            var resolver = new AutoPositionResolver(logger);

            // act
            var result = resolver.Resolve(symbol, orders, trades, startTime);

            // assert
            Assert.NotNull(result);
            Assert.Equal(symbol, result.Symbol);
            Assert.Empty(result.Positions);
            Assert.Collection(result.ProfitEvents, x =>
            {
                Assert.Equal(order1.OrderId, x.BuyOrderId);
                Assert.Equal(order2.OrderId, x.SellOrderId);
                Assert.Equal(trade1.Price, x.BuyPrice);
                Assert.Equal(trade2.Price, x.SellPrice);
                Assert.Equal(profit, x.Profit);
                Assert.Equal(trade2.Quantity - trade1.Commission, x.Quantity);
                Assert.Equal((trade1.Quantity - trade1.Commission) * trade1.Price, x.BuyValue);
                Assert.Equal((trade2.Quantity - trade1.Commission) * trade2.Price, x.SellValue);
                Assert.Equal(trade2.Time, x.EventTime);
                Assert.Same(symbol, x.Symbol);
            });
            Assert.Collection(result.CommissionEvents,
                x =>
                {
                    Assert.Equal(trade1.CommissionAsset, x.Asset);
                    Assert.Equal(trade1.OrderId, x.OrderId);
                    Assert.Equal(trade1.Id, x.TradeId);
                    Assert.Equal(trade1.Commission, x.Commission);
                    Assert.Equal(trade1.Time, x.EventTime);
                    Assert.Equal(trade1.Symbol, x.Symbol.Name);
                },
                x =>
                {
                    Assert.Equal(trade2.CommissionAsset, x.Asset);
                    Assert.Equal(trade2.OrderId, x.OrderId);
                    Assert.Equal(trade2.Id, x.TradeId);
                    Assert.Equal(trade2.Commission, x.Commission);
                    Assert.Equal(trade2.Time, x.EventTime);
                    Assert.Equal(trade2.Symbol, x.Symbol.Name);
                });
        }
    }
}