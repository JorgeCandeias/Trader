using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans;
using Orleans.TestingHost;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
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
        public async Task SetsAndGetsTrades()
        {
            // arrange
            var symbol = Guid.NewGuid().ToString();
            var trade1 = AccountTrade.Empty with { Symbol = symbol, Id = 1 };
            var trade2 = AccountTrade.Empty with { Symbol = symbol, Id = 2 };
            var trade3 = AccountTrade.Empty with { Symbol = symbol, Id = 3 };
            var trades = new[] { trade1, trade2, trade3 };
            var provider = _cluster.ServiceProvider.GetRequiredService<ITradeProvider>();

            // act
            await provider.SetTradesAsync(symbol, trades);
            var results = await provider.GetTradesAsync(symbol);

            // assert
            Assert.Equal(3, results.Count);
            Assert.Contains(trade1, results);
            Assert.Contains(trade2, results);
            Assert.Contains(trade3, results);
        }
    }
}