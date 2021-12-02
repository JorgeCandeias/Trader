using Moq;
using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Providers.Trades;
using Outcompute.Trader.Trading.Tests.Fixtures;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    [Collection(nameof(ClusterCollectionFixture))]
    public class TradeProviderTests
    {
        private readonly TestCluster _cluster;

        public TradeProviderTests(ClusterFixture cluster)
        {
            _cluster = cluster?.Cluster ?? throw new ArgumentNullException(nameof(cluster));
        }

        [Fact]
        public async Task SetsTrade()
        {
            // arrange
            var trade = AccountTrade.Empty with { Symbol = "ABCXYZ", Id = 123 };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITradeProviderReplicaGrain>(trade.Symbol, null).SetTradeAsync(trade))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var repository = Mock.Of<ITradingRepository>();
            var provider = new TradeProvider(factory, repository);

            // act
            await provider.SetTradeAsync(trade, CancellationToken.None);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsTrade()
        {
            // arrange
            var trade = AccountTrade.Empty with { Symbol = "ABCXYZ", Id = 123 };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITradeProviderReplicaGrain>(trade.Symbol, null).TryGetTradeAsync(trade.Id))
                .ReturnsAsync(trade)
                .Verifiable();

            var repository = Mock.Of<ITradingRepository>();
            var provider = new TradeProvider(factory, repository);

            // act
            var result = await provider.TryGetTradeAsync(trade.Symbol, trade.Id, CancellationToken.None);

            // assert
            Assert.Same(trade, result);
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task SetsTrades()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var trade1 = AccountTrade.Empty with { Symbol = symbol, Id = 1 };
            var trade2 = AccountTrade.Empty with { Symbol = symbol, Id = 2 };
            var trade3 = AccountTrade.Empty with { Symbol = symbol, Id = 3 };
            var trades = new[] { trade1, trade2, trade3 };

            var factory = Mock.Of<IGrainFactory>();
            Mock.Get(factory)
                .Setup(x => x.GetGrain<ITradeProviderReplicaGrain>(symbol, null).SetTradesAsync(trades))
                .Verifiable();

            var repository = Mock.Of<ITradingRepository>();

            var provider = new TradeProvider(factory, repository);

            // act
            await provider.SetTradesAsync(symbol, trades);

            // assert
            Mock.Get(factory).VerifyAll();
        }

        [Fact]
        public async Task GetsTrades()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var trade1 = AccountTrade.Empty with { Symbol = symbol, Id = 1 };
            var trade2 = AccountTrade.Empty with { Symbol = symbol, Id = 2 };
            var trade3 = AccountTrade.Empty with { Symbol = symbol, Id = 3 };
            var trades = new TradeCollection(new[] { trade1, trade2, trade3 });

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