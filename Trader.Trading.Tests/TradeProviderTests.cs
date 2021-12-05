using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Providers.Trades;

namespace Outcompute.Trader.Trading.Tests
{
    public class TradeProviderTests
    {
        [Fact]
        public async Task SetsTrade()
        {
            // arrange
            var symbol = "ABCXYZ";
            var trade = AccountTrade.Empty with { Id = 123, Symbol = symbol };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITradeProviderReplicaGrain>(symbol, null).SetTradeAsync(trade))
                .Verifiable();

            var repository = Mock.Of<ITradingRepository>();
            var provider = new TradeProvider(factory, repository);

            // act
            await provider.SetTradeAsync(trade);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsTrade()
        {
            // arrange
            var symbol = "ABCXYZ";
            var trade = AccountTrade.Empty with { Id = 123, Symbol = symbol };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITradeProviderReplicaGrain>(symbol, null).TryGetTradeAsync(trade.Id))
                .ReturnsAsync(trade)
                .Verifiable();

            var repository = Mock.Of<ITradingRepository>();
            var provider = new TradeProvider(factory, repository);

            // act
            var result = await provider.TryGetTradeAsync(symbol, trade.Id);

            // assert
            Assert.Same(trade, result);
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsTrades()
        {
            // arrange
            var symbol = "ABCXYZ";
            var trade = AccountTrade.Empty with { Id = 123, Symbol = symbol };
            var trades = new TradeCollection(new[] { trade });

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITradeProviderReplicaGrain>(symbol, null).GetTradesAsync())
                .ReturnsAsync(trades)
                .Verifiable();

            var repository = Mock.Of<ITradingRepository>();
            var provider = new TradeProvider(factory, repository);

            // act
            var result = await provider.GetTradesAsync(symbol);

            // assert
            Assert.Same(trades, result);
            Mock.Get(factory).VerifyAll();
        }
    }
}