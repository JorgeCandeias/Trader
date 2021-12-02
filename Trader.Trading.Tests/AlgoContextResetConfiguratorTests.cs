using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Algorithms.Context.Configurators;
using Outcompute.Trader.Trading.Algorithms.Positions;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoContextResetConfiguratorTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var configurator = new AlgoContextResetConfigurator();
            var name = "Algo1";
            var symbol = Symbol.Empty with { Name = "ABCXYZ" };
            var serviceProvider = NullServiceProvider.Instance;
            var context = new AlgoContext(name, serviceProvider);
            var item = context.Data.GetOrAdd("Item1");
            item.Symbol = Symbol.Empty with { BaseAsset = "ABC" };
            item.AutoPosition = AutoPosition.Empty with { Symbol = symbol };
            item.Ticker = MiniTicker.Empty with { Symbol = symbol.Name };
            item.Spot.BaseAsset = Balance.Empty with { Free = 123M };
            item.Spot.QuoteAsset = Balance.Empty with { Free = 123M };
            item.Savings.BaseAsset = SavingsBalance.Empty with { FreeAmount = 123M };
            item.Savings.QuoteAsset = SavingsBalance.Empty with { FreeAmount = 123M };
            item.SwapPools.BaseAsset = SwapPoolAssetBalance.Empty with { Total = 123M };
            item.SwapPools.QuoteAsset = SwapPoolAssetBalance.Empty with { Total = 123M };
            item.Orders.Completed = new OrderCollection(new[] { OrderQueryResult.Empty });
            item.Orders.Open = new OrderCollection(new[] { OrderQueryResult.Empty });
            item.Orders.Filled = new OrderCollection(new[] { OrderQueryResult.Empty });
            item.Trades = new TradeCollection(new[] { AccountTrade.Empty });
            item.Klines = new KlineCollection(new[] { Kline.Empty });
            item.Exceptions.Add(new InvalidOperationException());

            // act
            await configurator.ConfigureAsync(context, name);

            // assert
            Assert.Empty(item.Exceptions);
            Assert.Equal(Symbol.Empty, item.Symbol);
            Assert.Equal(AutoPosition.Empty, item.AutoPosition);
            Assert.Equal(MiniTicker.Empty, item.Ticker);
            Assert.Equal(Balance.Empty, item.Spot.BaseAsset);
            Assert.Equal(Balance.Empty, item.Spot.QuoteAsset);
            Assert.Equal(SwapPoolAssetBalance.Empty, item.SwapPools.BaseAsset);
            Assert.Equal(SwapPoolAssetBalance.Empty, item.SwapPools.QuoteAsset);
            Assert.Empty(item.Orders.Completed);
            Assert.Empty(item.Orders.Open);
            Assert.Empty(item.Orders.Filled);
            Assert.Empty(item.Trades);
            Assert.Empty(item.Klines);
        }
    }
}